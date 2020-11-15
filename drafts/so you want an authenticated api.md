---
layout: post
title: So you want an authenticated API
author: @recumbent
tags: F#, Authentication, Azure, Fable
---

It sounds simple, I want to have an authenticated API - I want to lock my functions down behind an API.

Authorization might be a good idea too, but that's the next bit of the story, this is about all the things I had to do to get from here to there.

## What pieces am I joining together

The backend is azure functions
There's an Azure API in front of the functions
The client is a straight up SPA, no intermediate web application.
At this point we are authenticating a single Azure AD tenant (B2B or B2C is for another day)

_notes:_
1. _I know we could authenticate directly against the functions in Azure, but in AWS we'd need to go through an API gateway and in any case the separation/abstraction is sensible._
1. _Some of this might be easier with an intermediate web server for the client, but most of the pieces would be the same._

## So how hard can it be?

Well... lets assume we want to play nice, lets further assume we're happy using Microsoft's ~~toybox~~ toolbox, given all that and the current "this what you should do" we get something along the lines of the following.

We - that is the applications we're authoring - don't own the users. We _might_ own some data about the users and things, but lets start with "we don't own". We do own AzureAD so there are some bits in there we can lean on.

To make an authenticated request it turns out that we need to do the following:

1. Log in to AzureAD - some form of magic
1. This gives us an identity, but probably doesn't give us access (I need to go revist some presentations) so...
1. Before we make a request we need to get an access token (using the ID token) our tokens are all JWT
1. We can then add our JWT to our request (Authorization header, as a Bearer token)
1. If the token is valid and meets all the defined criteria our request will go to the function and we'll get a response back. Yay!
1. Providing we can persuade all the CORS controls in the browser to actually let us do that.
1. And allowing that we want to let someone else do all the hard work of logging in and managing our logged in state (there's a library for that...)

To make this work at the server side, we need to attach a policy to our APIs in APIM to require authentication, we need to define an "application", we need to enable/authorise some relationship between a user or users defined in our AzureAD and that application, and we need to tell APIM - another policy - to play nice with CORS

We can do all this by clicking buttons... but actually we need to do all this in Pulumi. That's also for another day.

## ARM for unauthenticated and authenticated endpoints

### Unauthenticated

I've downloaded this as a zip, not sure if I should include it here

### Authenticated

I've downloaded this as a zip, not sure if I should include it here

## Actually do stuff...

So where to start?

### Set up the API

Left as an exercise for the reader, we shall assume you have some things that you want to protect and that they are sitting in API Management

### Create an Application Registration

Lets assume just one for now, it may be that there needs to be more than one or it may be that we can restrict things by other means - not sure for now.

Home > Directory > App registrations > New registration

Single Tenant because we're keeping this to ourselves for now

Branding would be nice, but is not necessary (will do some anyway just because)

#### Authentication

Need to add a platform, SPA... 

Redirect URL - start with http://localhost:8080 (which may not even be right for testing) - will need to be clever about this, either immediately or by adding cases after the fact

That's actually all we need in first instance

#### Expose an API

Can either accept the Application ID Uri - which is a GUID - or can change it to something that actually makes some kind of sense. I like to have things with meaningful names

Need to add scopes. For now we can pretty much just encode this - but we're going to need to be smarter somehow...

> A best practice is to use “resource.operation.constraint” as a pattern to generate the name.

In there we want to have some notion of per organisation scopes and those will be "runtime" created

Right now we need some variations on read/write/fetch

#### API permissions

Don't understand this properly yet. [Learn more about permissions and consent](https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent?WT.mc_id=Portal-Microsoft_AAD_RegisteredApps). In particular not entirely sure how to be selective / how to assign access per user.

If we grant everything to everyone it will all work, but really we'd like to be more measured. There's something in here where we need to manually grant things to people. Might actually be easier in Pulumi? We shall see.

Ah... in Home > Biosignatures Directory > Enterprise Applications > Quote API

Choose Properties, can then require user assignment, not sure if one can then limit access

#### Manifest

This is the manifest for my example that worked:

```json
{
	"id": "219fb087-a0ff-4a45-b193-0b5671f2cb5b",
	"acceptMappedClaims": null,
	"accessTokenAcceptedVersion": null,
	"addIns": [],
	"allowPublicClient": true,
	"appId": "6d277bb8-6151-404c-9a76-6fb0f8dbfc93",
	"appRoles": [],
	"oauth2AllowUrlPathMatching": false,
	"createdDateTime": "2020-10-20T10:06:45Z",
	"disabledByMicrosoftStatus": null,
	"groupMembershipClaims": null,
	"identifierUris": [
		"api://quote-api.biosigs"
	],
	"informationalUrls": {
		"termsOfService": null,
		"support": null,
		"privacy": null,
		"marketing": null
	},
	"keyCredentials": [],
	"knownClientApplications": [],
	"logoUrl": null,
	"logoutUrl": null,
	"name": "Quote API",
	"oauth2AllowIdTokenImplicitFlow": false,
	"oauth2AllowImplicitFlow": false,
	"oauth2Permissions": [
		{
			"adminConsentDescription": "Allows the user to fetch Douglas Adams quotes",
			"adminConsentDisplayName": "Get Douglas Adams quotes",
			"id": "e0ebc524-7571-4960-8f86-67e207a58831",
			"isEnabled": true,
			"lang": null,
			"origin": "Application",
			"type": "User",
			"userConsentDescription": "Allows you to fetch Douglas Adams quotes",
			"userConsentDisplayName": "adams",
			"value": "adams"
		},
		{
			"adminConsentDescription": "Allows the user to fetch Terry Pratchett quotes",
			"adminConsentDisplayName": "Fetch Terry Pratchett quotes",
			"id": "0977a449-29c2-49f7-ad03-297b9483a019",
			"isEnabled": true,
			"lang": null,
			"origin": "Application",
			"type": "User",
			"userConsentDescription": "Allow fetch of Terry Pratchett quotes",
			"userConsentDisplayName": "Fetch Pratchett Quotes",
			"value": "pratchett"
		}
	],
	"oauth2RequirePostResponse": false,
	"optionalClaims": null,
	"orgRestrictions": [],
	"parentalControlSettings": {
		"countriesBlockedForMinors": [],
		"legalAgeGroupRule": "Allow"
	},
	"passwordCredentials": [],
	"preAuthorizedApplications": [],
	"publisherDomain": "jacksonpopebiosignatures.onmicrosoft.com",
	"replyUrlsWithType": [
		{
			"url": "http://localhost:8090",
			"type": "Spa"
		}
	],
	"requiredResourceAccess": [
		{
			"resourceAppId": "6d277bb8-6151-404c-9a76-6fb0f8dbfc93",
			"resourceAccess": [
				{
					"id": "0977a449-29c2-49f7-ad03-297b9483a019",
					"type": "Scope"
				},
				{
					"id": "e0ebc524-7571-4960-8f86-67e207a58831",
					"type": "Scope"
				}
			]
		},
		{
			"resourceAppId": "00000003-0000-0000-c000-000000000000",
			"resourceAccess": [
				{
					"id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
					"type": "Scope"
				}
			]
		}
	],
	"samlMetadataUrl": null,
	"signInUrl": null,
	"signInAudience": "AzureADMyOrg",
	"tags": [
		"apiConsumer",
		"singlePageApp"
	],
	"tokenEncryptionKeyId": null
}
```

Might be worth playing with the "Integration Assistan | Preview" once this is set up to get insight into what our clients should look like. (There will be discussion of Fable below.)

### Securing the API

APIs are secured with policies. These are basically lumps of XML attached to the API

So at this point we have two basic requirements:

1. That the request is authenticated - basically that means that we have a valid, signed, token attached to the request
1. That the audience for the token matches the Application Registration above

For that we need to add something like the following to the `<inbound>`  policy at some level:

```xml
        <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="If only I knew why this failed">
            <openid-config url="https://login.microsoftonline.com/jacksonpopebiosignatures.onmicrosoft.com/.well-known/openid-configuration" />
            <audiences>
                <audience>api://quote-api.biosigs</audience>
            </audiences>
        </validate-jwt>
```

The two key pieces in the above are the openid-config - which deals iwth the signature (there may be other options?) - and the audience which corresponds to the Application ID Uri defined above. We can get the opend-id config by going to the app registration and looking at the endpoints. Not clear if we _have_ to use OpenID connect or whether we can use other endpoints. If the openid-config is _not_ set then the token cannot be validated so we're done...

...except CORS will cause us pain. So we also need the following - or at least _some_ of the follow, exactly how much I'm not sure

```xml
        <cors allow-credentials="true">
            <allowed-origins>
                <origin>http://localhost:8090</origin>
            </allowed-origins>
            <allowed-methods>
                <method>GET</method>
                <method>POST</method>
                <method>PUT</method>
                <method>DELETE</method>
                <method>HEAD</method>
                <method>OPTIONS</method>
                <method>PATCH</method>
                <method>TRACE</method>
            </allowed-methods>
            <allowed-headers>
                <header>*</header>
            </allowed-headers>
            <expose-headers>
                <header>*</header>
            </expose-headers>
        </cors>
```

The key bits in the above are:

* allow-credentials="true" - if we don't have this it fails because of the Authorization header
* allowed-origins - we can't wildcard this, so will have to work to ensure its up to date. Easier if we've dns'ed the public api endpoint. It may also be possible to parachute the incoming URL into this via params, not sure if that's a good idea though!
* allowed-methods - this can probably be thinned down but we're definitely going to need to retain options for browswer pre-flight
* allowed-headers - no idea, wildcard is possibly wrong haven't experimented yet
* expose-headers - same thing, not sure what we're exposing but we need at least some

I've got _outbound_ configuration too, but not sure if its necessary. I don't think it is given the above, but we can play later.

### Calling the API

MSAL... 

Ok, there are a bazillion articles about securing endpoints - to narrow things down a bit

1. We're working against a single tenant azure ad
2. We're writign a SPA (this should align quite well with native client applications)

So we're using [Microsoft Authentication Library (MSAL)](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-overview). More specifically we're using the broswer implementation [@azure/msal-browser](https://www.npmjs.com/package/@azure/msal-browser) as we're using a SPA.

Looking at the various examples we need to be able to do a very few things with MSAL:

1. Popup Auth Sign In - because the whole point is to be logged in!
1. Silent Access Token Request - access seems to be distinct from identity in the MS context, not sure how true this for other providers, but we can probably abstract that away at some point.
1. Sign Out - because we want to be good citizens

Then for, authenticated requests, we add a header: `Authorization: Bearer <JWT Token>`

And for all that it may _seem_ complex that's about it. To make this work there is a bit of configuration needed in the client and some other magic.

I used [ts2fable](https://github.com/fable-compiler/ts2fable)

The following are thinned down, we may need methods I haven't configured.

#### `PublicClientApplications.fs`

```fsharp
module rec Msal.PublicClientApplication

open System
open Fable.Core
open Fable.Core.JS
open Msal.Configuration
open Msal.AuthenticationResult
open Msal.Requests
open Msal.AccountInfo

type [<AllowNullLiteral>] IExports =
    abstract PublicClientApplication: PublicClientApplicationStatic

/// The PublicClientApplication class is the object exposed by the library to perform authentication and authorization functions in Single Page Applications
/// to obtain JWT tokens as described in the OAuth 2.0 Authorization Code Flow with PKCE specification.
type [<AllowNullLiteral>] PublicClientApplication =
    /// Use when initiating the login process via opening a popup window in the user's browser
    abstract loginPopup: ?request: PopupRequest -> Promise<AuthenticationResult>
    abstract logout: ?logoutRequest: EndSessionRequest -> Promise<unit>
    abstract acquireTokenSilent: request: SilentRequest -> Promise<AuthenticationResult>
    abstract getAccountByUsername: userName: string -> AccountInfo option
    abstract getAllAccounts: unit -> ResizeArray<AccountInfo>

/// The PublicClientApplication class is the object exposed by the library to perform authentication and authorization functions in Single Page Applications
/// to obtain JWT tokens as described in the OAuth 2.0 Authorization Code Flow with PKCE specification.

type [<AllowNullLiteral>] PublicClientApplicationStatic =
    [<Emit "new PublicClientApplication($1...)">] abstract Create: configuration: Configuration -> PublicClientApplication

[<Import("PublicClientApplication","@azure/msal-browser")>]
let publicClientApplication: PublicClientApplication.PublicClientApplicationStatic =
    jsNative
```

#### `AccountInfo.fs`

```fsharp
module rec Msal.AccountInfo

type [<AllowNullLiteral>] AccountInfo =
    abstract homeAccountId: string with get, set
    abstract environment: string with get, set
    abstract tenantId: string with get, set
    abstract username: string with get, set
    abstract name: string option with get, set
```

#### `AuthenticationResult.fs`

```fsharp
module rec Msal.AuthenticationResult

open System
open Msal.AccountInfo

type Array<'T> = System.Collections.Generic.IList<'T>

type [<AllowNullLiteral>] AuthenticationResult =
    abstract uniqueId: string with get, set
    abstract tenantId: string with get, set
    abstract scopes: Array<string> with get, set
    abstract account: AccountInfo with get, set
    abstract idToken: string with get, set
    abstract idTokenClaims: obj with get, set
    abstract accessToken: string with get, set
    abstract fromCache: bool with get, set
    abstract expiresOn: DateTime with get, set
    abstract tokenType: string with get, set
    abstract extExpiresOn: DateTime option with get, set
    abstract state: string option with get, set
    abstract familyId: string option with get, set
```

#### `Requests.fs`

```fsharp
// Consolidates things, might break opens in other snippets
module rec Msal.Requests

open Msal.AccountInfo

type [<AllowNullLiteral>] RedirectRequest =
    interface end

type [<AllowNullLiteral>] AuthorizationUrlRequest =
    interface end

type [<AllowNullLiteral>] SilentRequest =
    interface end

type [<AllowNullLiteral>] SsoSilentRequest =
    interface end

type PopupRequest =
    AuthorizationUrlRequest

type [<AllowNullLiteral>] EndSessionRequest =
    abstract account: AccountInfo option with get, set
    abstract postLogoutRedirectUri: string option with get, set
    abstract authority: string option with get, set
    abstract correlationId: string option with get, set
```

#### `Logger.fs`

(don't know how much I need this?)

```fsharp
module rec Msal.Logger

open Fable.Core

type [<AllowNullLiteral>] IExports =
    abstract Logger: LoggerStatic

type [<RequireQualifiedAccess>] LogLevel =
    | Error = 0
    | Warning = 1
    | Info = 2
    | Verbose = 3

type [<AllowNullLiteral>] LoggerMessageOptions =
    abstract logLevel: LogLevel with get, set
    abstract correlationId: string option with get, set
    abstract containsPii: bool option with get, set
    abstract context: string option with get, set

type [<AllowNullLiteral>] LoggerOptions =
    // abstract loggerCallback: ILoggerCallback option with get, set
    abstract piiLoggingEnabled: bool option with get, set
    abstract logLevel: LogLevel option with get, set

/// Callback to send the messages to.
type [<AllowNullLiteral>] ILoggerCallback =
    [<Emit "$0($1...)">] abstract Invoke: level: LogLevel * message: string * containsPii: bool -> unit

/// Class which facilitates logging of messages to a specific place.
type [<AllowNullLiteral>] Logger =
    /// Execute callback with message.
    abstract executeCallback: level: LogLevel * message: string * containsPii: bool -> unit
    /// Logs error messages.
    abstract error: message: string * ?correlationId: string -> unit
    /// Logs error messages with PII.
    abstract errorPii: message: string * ?correlationId: string -> unit
    /// Logs warning messages.
    abstract warning: message: string * ?correlationId: string -> unit
    /// Logs warning messages with PII.
    abstract warningPii: message: string * ?correlationId: string -> unit
    /// Logs info messages.
    abstract info: message: string * ?correlationId: string -> unit
    /// Logs info messages with PII.
    abstract infoPii: message: string * ?correlationId: string -> unit
    /// Logs verbose messages.
    abstract verbose: message: string * ?correlationId: string -> unit
    /// Logs verbose messages with PII.
    abstract verbosePii: message: string * ?correlationId: string -> unit
    /// Returns whether PII Logging is enabled or not.
    abstract isPiiLoggingEnabled: unit -> bool

/// Class which facilitates logging of messages to a specific place.
type [<AllowNullLiteral>] LoggerStatic =
    [<Emit "new $0($1...)">] abstract Create: loggerOptions: LoggerOptions -> Logger
```

#### `Configuration.fs`

```fsharp
module rec Msal.Configuration

open Fable.Core

type Array<'T> = System.Collections.Generic.IList<'T>

type [<StringEnum>] [<RequireQualifiedAccess>] CacheLocation =
    | LocalStorage
    | SessionStorage

type [<AllowNullLiteral>] IExports =
    /// MSAL function that sets the default options when not explicitly configured from app developer
    abstract buildConfiguration: p0: Configuration -> Configuration

type [<AllowNullLiteral>] BrowserAuthOptions =
    abstract clientId: string with get, set
    abstract authority: string option with get, set
    abstract knownAuthorities: Array<string> option with get, set
    abstract cloudDiscoveryMetadata: string option with get, set
    abstract redirectUri: string option with get, set
    abstract postLogoutRedirectUri: string option with get, set
    abstract navigateToLoginRequestUrl: bool option with get, set
    abstract clientCapabilities: Array<string> option with get, set

type [<AllowNullLiteral>] CacheOptions =
    abstract cacheLocation: string option with get, set
    abstract storeAuthStateInCookie: bool option with get, set

type [<AllowNullLiteral>] BrowserSystemOptions =
    interface end

type [<AllowNullLiteral>] Configuration =
    abstract auth: BrowserAuthOptions option with get, set
    abstract cache: CacheOptions option with get, set
    abstract system: BrowserSystemOptions option with get, set
```

#### `app.fs`

Thin this down to a less complex PoC

```fsharp
module App

open Fable.Core
open Fable.Core.JsInterop
open Elmish
open Browser
open Elmish.Navigation
open Elmish.UrlParser
open Fetch

open Msal.PopupRequest

// Types
type QueryState =
  | NotRequested
  | Loading
  | Success of string
  | Failure of string

type AuthState =
  | NotLoggedIn
  | LoginPending
  | LoggedIn of username : string
  | LogoutPending

type Model =
  { AppState : AuthState 
    FreeQueryState : QueryState
    AuthQueryState : QueryState
  }

type ApiEndpoint =
  | Free
  | Auth

type Msg = 
  | LogIn
  | LogInSuccess of string * string
  | LogOut
  | LogOutSuccess
  | Fetch of ApiEndpoint * string * int
  | FetchFailure of ApiEndpoint * string
  | FetchSuccess of ApiEndpoint * string

let author = "dna"
let quoteId = 9

// msal hackery, if I'm lucky...
let resourceUri = "https://biosigs-jm.azure-api.net"
let resourceScope = "api://quote-api.biosigs/adams"
let resourceScope2 = "api://quote-api.biosigs/pratchett"

let private msalConfig () : Msal.Configuration.Configuration =
  let authSettings:Msal.Configuration.BrowserAuthOptions =
    !!{|
        clientId = "6d277bb8-6151-404c-9a76-6fb0f8dbfc93"
        authority = Some "https://login.microsoftonline.com/5ac57dde-a080-4b71-a471-b3e9db4a13a9"
        validateAuthority = true
        redirectUri = (Some window.location.origin)
        postLogoutRedirectUri = (Some window.location.origin)
        navigateToLoginRequestUrl = Some true
    |}


  let cacheSettings:Msal.Configuration.CacheOptions =
    !!{|
        cacheLocation = Some Msal.Configuration.CacheLocation.LocalStorage
    |}

  let sys : Msal.Configuration.BrowserSystemOptions option = None
  let config:Msal.Configuration.Configuration =
    !!{|
      auth = authSettings
      cache = Some cacheSettings
      system = sys
    |}

  config

let config = msalConfig ()
let clientApp = Msal.PublicClientApplication.publicClientApplication.Create(config)

let fetchToken username =
    promise {
        let account = clientApp.getAccountByUsername username
        printfn "Account info: %O" account
        let req : Msal.SilentRequest.SilentRequest = 
            !!{|
                account = account
                scopes = Array.ofList [ resourceScope ]
            |}

        let! tokenResponse = clientApp.acquireTokenSilent(req)
        return tokenResponse.accessToken
    } 


let get (endpoint, username, author, queryId) =
    promise {
        // TODO: Yeah, no...
        let! token =
          match username with
          | Some n ->
            promise { 
              let! tk = fetchToken n
              return Some tk
            }
          | None ->
            promise {
              return None
            }
        
        // TODO: Pass the right endpoint in somewhere...
        let mode = 
            match endpoint with
            | Free -> "free"
            | Auth -> "auth"

        let headers = 
            match endpoint with
            | Free -> requestHeaders []
            | Auth -> 
                let auth = Option.map (sprintf "Bearer %s") token
                let headers = 
                  auth
                  |> Option.map Authorization
                  |> Option.toList
                requestHeaders headers

        let url = sprintf "%s/%s/quote/%s/%i" resourceUri mode author queryId  // TODO: Add author and quoteId

        // TODO Only add auth for auth endpoint - refactor a bit to make that work
        let! response = fetch url [ headers ] // [Mode RequestMode.Nocors]  // [ requestHeaders [ Authorization auth ] ] 
        return! response.text()
    }

let signIn _ =
    promise {
        let req : PopupRequest = 
           !!{|
               scopes = Array.ofList [ resourceScope ]
           |}

        let! authResult = clientApp.loginPopup(req)

        return (authResult.account.username, authResult.idToken)
    }

let signOut () =
    promise {
        printfn "Logging out"
        return! clientApp.logout()
    }

let init() = 
  let accounts = clientApp.getAllAccounts()
  let initState =
      if (accounts.Count > 0)
      then 
        let account = accounts.[0]
        LoggedIn account.username
      else
        NotLoggedIn
  ({ AppState = initState; FreeQueryState = NotRequested; AuthQueryState = NotRequested }, Cmd.none)

let update msg model =
  let username =
      match model.AppState with
      | NotLoggedIn
      | LoginPending
      | LogoutPending     -> None
      | LoggedIn userName -> Some userName

  match msg with
  | LogIn -> 
      { model with AppState = LoginPending }, 
      Cmd.OfPromise.either signIn () (LogInSuccess) (fun _ -> LogOutSuccess)

  | LogInSuccess (username, token) ->
      { model with AppState = LoggedIn username }, Cmd.none

  | LogOut ->
      { model with AppState = LogoutPending }, Cmd.OfPromise.perform signOut () (fun _ -> LogOutSuccess)

  | FetchFailure (endpoint, s) ->
        let model = 
            match endpoint with
              | Free -> { model with FreeQueryState = Failure s }
              | Auth -> { model with AuthQueryState = Failure s }
        model, Cmd.none

  | FetchSuccess (endpoint, s) -> 
        let model = 
            match endpoint with
              | Free -> { model with FreeQueryState = Success s }
              | Auth -> { model with AuthQueryState = Success s }
        model, Cmd.none

  | Fetch (endpoint, author, id) ->
      let model = 
        match endpoint with
        | Free -> { model with FreeQueryState = Loading }
        | Auth -> { model with AuthQueryState = Loading }
      model, Cmd.OfPromise.either get (endpoint, username, author, quoteId) (fun x -> FetchSuccess (endpoint, x)) (fun x -> FetchFailure (endpoint, x.ToString()))

  | LogOutSuccess ->
      { model with AppState = NotLoggedIn }, Cmd.none

// VIEW

open Fable.React
open Fable.React.Props

let internal centerStyle direction =
    Style [ Display DisplayOptions.Flex
            FlexDirection direction
            AlignItems AlignItemsOptions.Center
            unbox("justifyContent", "center")
            Padding "20px 0" ]

let words size message =
  span [ Style [ unbox("fontSize", size |> sprintf "%dpx") ] ] [ str message ]
  
open Fable.Core.JsInterop

let viewContent endpoint model dispatch =
  let fetchFunc = (fun _ -> dispatch (Fetch (endpoint, author, quoteId)))
  match model with
  | NotRequested ->
      [ h2 [] [ str "Not Requested" ] 
        hr []
        button [OnClick fetchFunc] [str "Make request"]
      ]
  | Loading -> 
      [ h2 [] [ str "Loading"] ]
  | Success s -> 
      [ h2 [] [ str "Success"] 
        p [] [ str s ]
        hr []
        button [OnClick fetchFunc] [str "Make request"]
      ]
  | Failure s -> 
      [ h2 [] [ str "Failure"]
        p [] [ str s ]
        hr []
        button [OnClick fetchFunc] [str "Make request"]
      ]

let view model dispatch =
  div []
    [ h1 [] [str "Fable fetch hackery"]
      hr []
      match model.AppState with
      | NotLoggedIn ->
        div [] [ h2 [] [ str "Not logged in"]]
        button [OnClick (fun _ -> dispatch LogIn)] [str "Login"]
      | LoginPending ->
        div [] [ h2 [] [ str "Log in pending"]]
      | LoggedIn username ->
        div [] [ 
          p [] [ str username ]
          ]
        button [OnClick (fun _ -> dispatch LogOut)] [str "Logout"]
      | LogoutPending ->
        div [] [ h2 [] [ str "Log out pending"]]

      hr []
      h3 [] [ str "Free endpoint" ]
      div [] (viewContent Free model.FreeQueryState dispatch)
      hr []
      h3 [] [ str "Auth endpoint" ]
      div [] (viewContent Auth model.AuthQueryState dispatch)
      hr []
    ]

open Elmish.React
open Elmish.Debug

// App
Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.withDebugger
|> Program.run 
```

#### `.fs`

```fsharp
```