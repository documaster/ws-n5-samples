<?php
require('vendor/autoload.php');
require('./config.php');



$opts = getopt(null, array("enonic-article-id:") );
print_r($opts);
if(!isset($opts['enonic-article-id'])) {
    die("Usage: php " . basename(__FILE__) . " --enonic-article-id=1234\n\n");
}
$enonic_article_id = $opts['enonic-article-id'];


$auth_info = @file_get_contents("auth.json");
if($auth_info !== FALSE) {
    $auth_info = json_decode($auth_info);
}

// Authenticate if token is missing or is about to expire
if($auth_info === FALSE || time() > $auth_info->expires_at) {
    print("Authenticating...");

    $idp_client = new GuzzleHttp\Client(['base_uri' => "https://$DOMAIN"]);

    $response = $idp_client->request('POST', '/idp/oauth2/token', [
        //'body' => "grant_type=password&username=$USER&password=$PASS&scope=openid",
        'form_params' => [
            'grant_type' => 'password',
            'username' => $USER,
            'password' => $PASS,
            'scope' => 'openid'
        ],
        'auth' => [$CLIENT_ID, $CLIENT_SECRET]
    ]);

    if($response->getStatusCode() == 200) {
        print("Authenticated\n");
        $json = (string) $response->getBody();
        $auth_info = json_decode($json);

        // Add expiry timestamp so we can check it later
        $auth_info->expires_at = time() + $auth_info->expires_in - 60;

        // Cache token to file
        file_put_contents("auth.json", json_encode($auth_info));
    }
} else {
    print("Using cached authentication token\n");
}




$rms_client = new GuzzleHttp\Client(['base_uri' => "https://$DOMAIN:8083"]);
$create_class_operations = "";
$link_class_operations = "";

$new_icd_10_class_id                = find_or_create_class($CLASSIFICATION_SYSTEM_ICD_10, 'A082', 'Adenovirusenteritt');


$json_file_id = upload_file('json_testfil.json');
$pdf_file_id = upload_file('Pdf_testfil.pdf');

$now = date(DATE_ATOM);
$eksterntSystem = "Enonic";

// Find the article versions we are updating based on enonic article id stored in the ExtId object

$registrering_query='{
    "type": "Basisregistrering",
    "offset": "0",
    "limit": "1000",
    "query": "refMappe.refEksternId.eksterntSystem = @eksterntSystem && refMappe.refEksternId.eksternID = @eksternID",
    "parameters":
      {
        "@eksterntSystem": "' . $eksterntSystem . '",
        "@eksternID": "' . $enonic_article_id . '"
      },
    "publicUse": "false"
  }';


$response = $rms_client->request('POST', 'rms/api/public/noark5/v1/query', [
    'headers' => [
        'Accept'            => 'application/json',
        'Authorization'     => 'Bearer ' . $auth_info->access_token,
        'Content-Type'      => 'application/json'
    ],
    'body' => $registrering_query
]);


$registreringer = null;
if($response->getStatusCode() == 200) {
    $json = (string) $response->getBody();
    $query_result = json_decode($json);

    print_r($query_result);

    if(count($query_result->results) > 0) {
        print("Found existing folder for article $enonic_article_id\n");
        $registreringer = $query_result->results;
    } else {
        die("Could not find folder for article $enonic_article_id\n");
    }
} else {
    die("Folder query failed");
}

// Compare function for sorting registreringer by version number descending
function compare_registrering($a, $b) {
    $a_version = explode('.', $a->fields->virksomhetsspesifikkeMetadata->dip->versjon->values[0]);
    $b_version = explode('.', $b->fields->virksomhetsspesifikkeMetadata->dip->versjon->values[0]);
    if(count($a_version) != count($b_version)) die("Version numbers are not compatible");

    if($a_version[0] == $b_version[0]) {
        if($a_version[1] == $b_version[1]) {
            return 0;
        }
        return (intval($a_version[1]) < intval($b_version[1])) ? 1 : -1;
    }

    return (intval($a_version[0]) < intval($b_version[0])) ? 1 : -1;
}



usort($registreringer, "compare_registrering");


$last_registrering = $registreringer[0];
$last_version = explode('.', $last_registrering->fields->virksomhetsspesifikkeMetadata->dip->versjon->values[0]);
$next_version = implode('.',array(strval(intval($last_version[0]) + 1), "0"));
$mappe_id = $last_registrering->links->refMappe;
$registrering_id = uniqid();
$dokument_id = uniqid();
$dokumentversjon_json_id = uniqid();
$dokumentversjon_pdf_id = uniqid();
$new_version_tittel = "Ny artikkelversjon sin tittel (" . date("Y-m-d H:i:s") . ")"; 

// Check if new secondary class is already linked to folder
$klass_query='{
    "type": "Klasse",
    "offset": "0",
    "limit": "100",
    "query": "refMappeSomSekundaer.id = @mappeId",
    "parameters":
      {
        "@mappeId": "' . $mappe_id . '"
      },
    "publicUse": "false"
  }';


$klass_response = $rms_client->request('POST', 'rms/api/public/noark5/v1/query', [
    'headers' => [
        'Accept'            => 'application/json',
        'Authorization'     => 'Bearer ' . $auth_info->access_token,
        'Content-Type'      => 'application/json'
    ],
    'body' => $klass_query
]);

if($klass_response->getStatusCode() == 200) {
    $json = (string) $klass_response->getBody();
    $query_result = json_decode($json);

    print($json);
    //print_r($query_result);

    if(count($query_result->results) > 0) {
        print("Secondary class is already linked\n");
    } else {
        $link_class_operations .= '{
            "action": "link",
            "type": "Mappe",
            "id": "' . $mappe_id . '",
            "ref": "refSekundaerKlasse",
            "linkToId": "' . $new_icd_10_class_id . '"
          },';
    }
} else {
    die("Klasse query failed");
}





$transaction = '{
    "actions": [
      ' . $create_class_operations . '
      {
        "action": "save",
        "type": "Mappe",
        "id": "' . $mappe_id . '",
        "fields": {
          "tittel": "' . $new_version_tittel . '"
        }
      },
      ' . $link_class_operations . '
      {
        "action": "save",
        "type": "Basisregistrering",
        "id": "' . $registrering_id . '",
        "fields": {
          "tittel": "' . $new_version_tittel . " " . $next_version . '",
          "beskrivelse": "Intro eller kortintro",
          "forfatter": "Karl Karlsen",
          "dokumentmedium": "E",
          "registreringsDato": "2018-10-03",
          "virksomhetsspesifikkeMetadata": {
            "dip": {
              "versjon": {"values": ["' . $next_version . '"] },
              "infotype": {"values": ["Info type"] },
              "malgruppe": {"values": ["Målgruppe"] },
              "sprak": {"values": ["Språk"] },
              "intro": {"values": ["Intro"] },
              "kortintro": {"values": ["Kort intro"] },
              "korttittel": {"values": ["Kort tittel"] }
            }
          }
        }
      },
      {
        "action": "link",
        "type": "Basisregistrering",
        "id": "' . $registrering_id . '",
        "ref": "refMappe",
        "linkToId": "' . $mappe_id . '"
      },
      {
        "action": "save",
        "type": "Basisregistrering",
        "id": "' . $registrering_id . '",
        "fields": {
            "opprettetDato": "' . $now . '", 
            "opprettetAv": "Karl Karlsen",                  
            "opprettetAvBrukerIdent": "kkarlsen"
        }
      },
      {
        "action": "save",
        "type": "Dokument",
        "id": "' . $dokument_id . '",
        "fields": {
          "dokumenttype": "PUBLIKASJON",
          "dokumentstatus": "B",
          "tittel": "Denne versjon av artikkelen sin tittel",
          "beskrivelse": "Intro eller kortintro",
          "forfatter": "Navn på forfatter",
          "tilknyttetRegistreringSom": "H",
          "dokumentnummer": ' . explode(".", $next_version)[0] . '
        }
      },
      {
        "action": "link",
        "type": "Dokument",
        "id": "' . $dokument_id . '",
        "ref": "refRegistrering",
        "linkToId": "' . $registrering_id . '"
      },
      {
        "action": "save",
        "type": "Dokument",
        "id": "' . $dokument_id . '",
        "fields": {
            "opprettetDato": "' . $now . '", 
            "opprettetAv": "Karl Karlsen",                  
            "opprettetAvBrukerIdent": "kkarlsen"
        }
      },
      {
        "action": "save",
        "type": "Dokumentversjon",
        "id": "' . $dokumentversjon_json_id . '",
        "fields": {
          "variantformat": "P",
          "format": "json",
          "formatDetaljer": "application/json",
          "versjonsnummer" : "1",
          "referanseDokumentfil": ' . $json_file_id . '
        }
      },
      {
        "action": "link",
        "type": "Dokumentversjon",
        "id": "' . $dokumentversjon_json_id . '",
        "ref": "refDokument",
        "linkToId": "' . $dokument_id . '"
      },
      {
        "action": "save",
        "type": "Dokumentversjon",
        "id": "' . $dokumentversjon_json_id . '",
        "fields": {
            "opprettetDato": "' . $now . '", 
            "opprettetAv": "Karl Karlsen",                  
            "opprettetAvBrukerIdent": "kkarlsen"
        }
      },
      {
        "action": "save",
        "type": "Dokumentversjon",
        "id": "' . $dokumentversjon_pdf_id . '",
        "fields": {
          "variantformat": "A",
          "format": "pdf",
          "formatDetaljer": "application/pdf",
          "versjonsnummer" : "2",
          "referanseDokumentfil": ' . $pdf_file_id . '
        }
      },
      {
        "action": "link",
        "type": "Dokumentversjon",
        "id": "' . $dokumentversjon_pdf_id . '",
        "ref": "refDokument",
        "linkToId": "' . $dokument_id . '"
      },
      {
        "action": "save",
        "type": "Dokumentversjon",
        "id": "' . $dokumentversjon_pdf_id . '",
        "fields": {
            "opprettetDato": "' . $now . '", 
            "opprettetAv": "Karl Karlsen",                  
            "opprettetAvBrukerIdent": "kkarlsen"
        }
      },' . 
      close_registrering_actions($last_registrering->id)
      . '
      
    ]
  }';


  print("Executing transaction...");
  print($transaction . "\n\n");
  $response = $rms_client->request('POST', 'rms/api/public/noark5/v1/transaction', [
    'headers' => [
        'X-Documaster-Error-Response-Type'            => 'application/json',
        'Accept'            => 'application/json',
        'Authorization'     => 'Bearer ' . $auth_info->access_token,
        'Content-Type'      => 'application/json'
    ],
    'body' => $transaction
]);

if($response->getStatusCode() == 200) {
    print("Transaction succeeded!\n");
    $json = (string) $response->getBody();
    $transaction_result = json_decode($json);

    $saved_mappe = $transaction_result->saved->$mappe_id;

    print("Archived article with enonic id '$enonic_article_id' into Documaster folder with id " . $saved_mappe->id . "\n");
    
} else {
    print("Transaction failed:\n");
    print( $response->getBody() );
}




function find_folder_by_extid($system, $extid) {
    GLOBAL $rms_client, $auth_info;

    $klass_query='{
        "type": "Klasse",
        "offset": "0",
        "limit": "100",
        "query": "refKlassifikasjonssystem.id = @klassificationsystemid && klasseIdent = @klasseIdent",
        "parameters":
          {
            "@klassificationsystemid": "' . $classification_system_id . '",
            "@klasseIdent": "' . $class_ident . '"
          },
        "publicUse": "false"
      }';
    

    $response = $rms_client->request('POST', 'rms/api/public/noark5/v1/query', [
        'headers' => [
            'Accept'            => 'application/json',
            'Authorization'     => 'Bearer ' . $auth_info->access_token,
            'Content-Type'      => 'application/json'
        ],
        'body' => $klass_query
    ]);

    if($response->getStatusCode() == 200) {
        $json = (string) $response->getBody();
        $query_result = json_decode($json);
        if(count($query_result->results) > 0) {
            print("Found existing class $class_ident\n");
            return $query_result->results[0]->id;
        }
    }

    // Class was not found. Create it
    print("Class $class_ident was not found. Creating new class in classification system.\n");

  
    return null;
}


function find_or_create_class($classification_system_id, $class_ident, $class_tittel) {
    GLOBAL $rms_client, $auth_info, $create_class_operations;

    $klass_query='{
        "type": "Klasse",
        "offset": "0",
        "limit": "100",
        "query": "refKlassifikasjonssystem.id = @klassificationsystemid && klasseIdent = @klasseIdent",
        "parameters":
          {
            "@klassificationsystemid": "' . $classification_system_id . '",
            "@klasseIdent": "' . $class_ident . '"
          },
        "publicUse": "false"
      }';
    

    $response = $rms_client->request('POST', 'rms/api/public/noark5/v1/query', [
        'headers' => [
            'Accept'            => 'application/json',
            'Authorization'     => 'Bearer ' . $auth_info->access_token,
            'Content-Type'      => 'application/json'
        ],
        'body' => $klass_query
    ]);

    if($response->getStatusCode() == 200) {
        $json = (string) $response->getBody();
        $query_result = json_decode($json);
        if(count($query_result->results) > 0) {
            print("Found existing class $class_ident\n");
            return $query_result->results[0]->id;
        }
    }

    // Class was not found. Create it
    print("Class $class_ident was not found. Creating new class in classification system.\n");

    $new_class_id = uniqid();

    $create_class_operations .= '{
      "action": "save",
      "type": "Klasse",
      "id": "' . $new_class_id . '",
      "fields": {
        "klasseIdent": "' . $class_ident . '",
        "tittel": "' . $class_tittel . '"
      }
    },
    {
      "action": "link",
      "type": "Klasse",
      "id": "' . $new_class_id . '",
      "ref": "refKlassifikasjonssystem",
      "linkToId": "' . $classification_system_id . '"
    },';

    return $new_class_id;
}



function upload_file($filepath) {
    GLOBAL $rms_client, $auth_info;

    print("Uploading file '$filepath'...");

    $filename = basename($filepath);
    
    $file = fopen($filepath, 'r');
    $response = $rms_client->request('POST', 'rms/api/public/noark5/v1/upload', [
        'headers' => [
            'Accept'            => 'application/json',
            'Authorization'     => 'Bearer ' . $auth_info->access_token,
            'Content-Type'      => 'application/octet-stream',
            'Content-Disposition' => "attachment; filename*=utf-8''" . urlencode($filename) 
        ],
        'body' => $file
        

    ]);

    

    if($response->getStatusCode() == 200) {
        print("done\n");
        $json = (string) $response->getBody();
        $result = json_decode($json);
        return $result->id;
    } else {
        print("failed\n");
    }

    return null;
}

function close_registrering_actions($close_registrering_id) {
    GLOBAL $rms_client, $auth_info, $now;
    $actions = "";

    // Close basisregistrerring
    print("Closing Basisregistrering " . $close_registrering_id . "\n");
    $actions .= '{
        "action": "save",
        "type": "Basisregistrering",
        "id": "' . $close_registrering_id . '",
        "fields": {
            "avsluttetDato": "' . $now . '", 
            "avsluttetAv": "Karl Karlsen",                  
            "avsluttetAvBrukerIdent": "kkarlsen"
        }
      }';

    //Find and close Dokument
    $dokument_query='{
        "type": "Dokument",
        "offset": "0",
        "limit": "1000",
        "query": "refRegistrering.id = @registreringId",
        "parameters":
        {
            "@registreringId": "' . $close_registrering_id . '"
        },
        "publicUse": "false"
    }';


    $dokument_response = $rms_client->request('POST', 'rms/api/public/noark5/v1/query', [
        'headers' => [
            'Accept'            => 'application/json',
            'Authorization'     => 'Bearer ' . $auth_info->access_token,
            'Content-Type'      => 'application/json'
        ],
        'body' => $dokument_query
    ]);

    if($dokument_response->getStatusCode() == 200) {
        $json = (string) $dokument_response->getBody();
        $query_result = json_decode($json);
        foreach($query_result->results as $dokument) {
            print("Closing Dokument " . $dokument->id . "\n");
            $actions .= ',
              {
                "action": "save",
                "type": "Dokument",
                "id": "' . $dokument->id . '",
                "fields": {
                    "dokumentstatus": "F"
                }
              }';
        }

    } else {
        die("Klasse query failed");
    }

    return $actions;
}