<?php
require('vendor/autoload.php');
require('./config.php');

// Read cached token from file
$auth_info = @file_get_contents("auth.json");
if($auth_info !== FALSE) {
    $auth_info = json_decode($auth_info);
}

// Authenticate if token is missing or is about to expire
if($auth_info === FALSE || time() > $auth_info->expires_at) {
    print("Authenticating...");

    $idp_client = new GuzzleHttp\Client(['base_uri' => "https://$DOMAIN"]);

    $response = $idp_client->request('POST', '/idp/oauth2/token', [
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
        // Subtracting 60 seconds to make sure it does not expire during the request.
        $auth_info->expires_at = time() + $auth_info->expires_in - 60;

        // Write token information to file
        file_put_contents("auth.json", json_encode($auth_info));
    }
} else {
    print("Using cached authentication token\n");
}




$rms_client = new GuzzleHttp\Client(['base_uri' => "https://$DOMAIN:8083"]);
$create_class_operations = "";

$publikasjonstyper_klass_id     = find_or_create_class($CLASSIFICATION_SYSTEM_PUBLIKASJONSTYPER, 'artikkel', 'Publisere artikkel');
$mesh_class_id                  = find_or_create_class($CLASSIFICATION_SYSTEM_MESH, 'A01.456.313', 'Øre');
$icd_10_class_id                = find_or_create_class($CLASSIFICATION_SYSTEM_ICD_10, 'G44.0', 'Clusterhodepine-syndrom');
$icpc_2_class_id                = find_or_create_class($CLASSIFICATION_SYSTEM_ICPC_2, 'A88', 'Skadevirkning av fysisk faktor');

/*
Upload the document files as temporary files. 
Keep the id's for linking them to the Dokumentversjon objects.

NOTE!: If these files are not linked to a Dokumentversjon object in the metadata they will
be removed automatically by a cleanup job on the Documaster server.
*/
$json_file_id = upload_file('json_testfil.json');
$pdf_file_id = upload_file('Pdf_testfil.pdf');

$now = date(DATE_ATOM);

$enonic_article_id = uniqid("Enonic");

// Create temporary id's for new objects in the transaction
$mappe_id                   = uniqid();
$eksternId_id               = uniqid();
$registrering_id            = uniqid();
$dokument_id                = uniqid();
$dokumentversjon_json_id    = uniqid();
$dokumentversjon_pdf_id     = uniqid();

$versjonsnummer = "1.0";


/*
Prepare a big transaction that creates the entire metadata structure in one call to the server API

Note that system managed fields (opprettetDato, opprettetAv, opprettetAvBrukerIdent...) have to be saved
in a separate operation after the object is linked to it's parent object.
This is because the permission checks needs to know it's location in the structure before they can grant
permission to write to these fields

*/
$transaction = '{
    "actions": [
      ' . $create_class_operations . '
      {
        "action": "save",
        "type": "Mappe",
        "id": "' . $mappe_id . '",
        "fields": {
          "tittel": "Artikkelens tittel (oppdateres hver gang artikkelen endrer tittel)",
          "beskrivelse": "Intro eller kortintro tekst",
          "dokumentmedium": "E"
        }
      },
      {
        "action": "link",
        "type": "Mappe",
        "id": "' . $mappe_id . '",
        "ref": "refPrimaerKlasse",
        "linkToId": "' . $publikasjonstyper_klass_id . '"
      },
      {
        "action": "link",
        "type": "Mappe",
        "id": "' . $mappe_id . '",
        "ref": "refSekundaerKlasse",
        "linkToId": "' . $mesh_class_id . '"
      },
      {
        "action": "link",
        "type": "Mappe",
        "id": "' . $mappe_id . '",
        "ref": "refSekundaerKlasse",
        "linkToId": "' . $icd_10_class_id . '"
      },
      {
        "action": "link",
        "type": "Mappe",
        "id": "' . $mappe_id . '",
        "ref": "refSekundaerKlasse",
        "linkToId": "' . $icpc_2_class_id . '"
      },
      {
        "action": "link",
        "type": "Mappe",
        "id": "' . $mappe_id . '",
        "ref": "refArkivdel",
        "linkToId": "' . $ARKIVDEL_ID . '"
      },
      {
        "action": "save",
        "type": "Mappe",
        "id": "' . $mappe_id . '",
        "fields": {
          "opprettetDato": "' . $now . '", 
          "opprettetAv": "Karl Karlsen",                  
          "opprettetAvBrukerIdent": "kkarlsen"
        }
      },
      {
        "action": "save",
        "type": "EksternId",
        "id": "' . $eksternId_id . '",
        "fields": {
          "eksterntSystem": "Enonic",
          "eksternID": "' . $enonic_article_id . '"
        }
      },
      {
        "action": "link",
        "type": "EksternId",
        "id": "' . $eksternId_id . '",
        "ref": "refMappe",
        "linkToId": "' . $mappe_id . '"
      },
      {
        "action": "save",
        "type": "Basisregistrering",
        "id": "' . $registrering_id . '",
        "fields": {
          "tittel": "Denne versjon av artikkelen sin tittel ' . $versjonsnummer . '",
          "beskrivelse": "Intro eller kortintro",
          "forfatter": "Karl Karlsen",
          "dokumentmedium": "E",
          "registreringsDato": "2018-10-03",
          "virksomhetsspesifikkeMetadata": {
            "dip": {
              "versjon": {"values": ["' . $versjonsnummer . '"] },
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
          "dokumentnummer": ' . explode(".", $versjonsnummer)[0] . '
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
      } 
    ]
  }';
  
  print("Executing transaction...");
  //print($transaction . "\n\n");
  $response = $rms_client->request('POST', 'rms/api/public/noark5/v1/transaction', [
    'headers' => [
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
