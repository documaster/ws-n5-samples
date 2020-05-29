using CommandLine;
using Documaster.WebApi.Client.IDP;
using Documaster.WebApi.Client.IDP.Oauth2;
using Documaster.WebApi.Client.Noark5.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoarkWsClientSample
{
    public class DocumasterClients
    {
        private NoarkClient noarkClient;
        private Oauth2HttpClient idpClient;

        private string refreshToken;
        private DateTime accessTokenExpirationTime;
        private readonly Options opts;

        public DocumasterClients(Options opts)
        {
            this.opts = opts;

            /*
             * Using the Noark 5 web services requires providing a valid access token.
             * The way this token is obtained depends on the system implementing the services.
             * This sample code obtains the token from the Documaster's identity provider service with the help of a designated Documaster IDP client.
             * If the c# Noark client is used in the context of an application that has access to a web browser,
             * we strongly recommend choosing the Oauth2 Authorization Code Grant Flow to avoid providing username and password.
             * The c# Noark client provides both synchronous and 'async' methods.
             * The 'async' methods should be called if the client is used in the context of a an application with an user interface.
             * The synchronous methods can be used in console applications and libraries.
             */

            //Initialize an IDP client and request an authorization token
            InitIdpClient(opts);

            //Initialize a Noark client
            InitNoarkClient(opts);
        }

        public NoarkClient GetNoarkClient()
        {
            RefreshAccessToken();
            return this.noarkClient;
        }

        private void RefreshAccessToken()
        {
            //access token expires in 60 minutes

            if (this.refreshToken == null)
            {
                PasswordGrantTypeParams passwordGrantTypeParams = new PasswordGrantTypeParams(this.opts.ClientId,
                    this.opts.ClientSecret, this.opts.Username, this.opts.Password, OpenIDConnectScope.OPENID);
                AccessTokenResponse accessTokenResponse =
                    this.idpClient.GetTokenWithPasswordGrantType(passwordGrantTypeParams);
                this.accessTokenExpirationTime = DateTime.Now.AddSeconds(accessTokenResponse.ExpiresInMs);
                this.refreshToken = accessTokenResponse.RefreshToken;
                this.noarkClient.AuthToken = accessTokenResponse.AccessToken;
            }
            else if (DateTime.Now > this.accessTokenExpirationTime.AddSeconds(-20))
            {
                RefreshTokenGrantTypeParams refreshTokenGrantTypeParams =
                    new RefreshTokenGrantTypeParams(this.refreshToken, this.opts.ClientId, this.opts.ClientSecret,
                        OpenIDConnectScope.OPENID);
                AccessTokenResponse accessTokenResponse = this.idpClient.RefreshToken(refreshTokenGrantTypeParams);
                this.accessTokenExpirationTime = DateTime.Now.AddSeconds(accessTokenResponse.ExpiresInMs);
                this.refreshToken = accessTokenResponse.RefreshToken;
                this.noarkClient.AuthToken = accessTokenResponse.AccessToken;
            }
        }

        private void InitIdpClient(Options options)
        {
            //IdpServerAddress is in the format https://clientname.dev.documaster.tech/idp/oauth2
            this.idpClient = new Oauth2HttpClient(options.IdpServerAddress, true);
        }

        private void InitNoarkClient(Options options)
        {
            //ServerAddress is in the format https://clientname.dev.documaster.tech:8083
            this.noarkClient = new NoarkClient(options.ServerAddress, true);
        }
    }
}
