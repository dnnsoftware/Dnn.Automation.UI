module DnnWebApi

open System
open System.Collections.Generic
open System.Text
open System.Net
open HttpFs.Client
open Newtonsoft.Json
open canopy
open DnnUserLogin

let customHeader (x: NameValuePair) = Custom(x.Name,x.Value)

let customCookie (x: NameValuePair) = Cookie.create(x.Name,x.Value)

let jsonContentTypeHeader = ContentType (ContentType.create("application", "json", Encoding.UTF8))

let formUrlEncodedContentTypeHeader = ContentType (ContentType.create("application","x-www-form-urlencoded", Encoding.UTF8))

/// <summary>
/// Forms a full Uri for an url nad 
/// </summary>
/// <param name="url">The HTTP/HTTP url for the service</param>
/// <param name="resource">The resource within the service</param>
let uriFor (url : string) (resource : string) = 
    let uri = Uri(sprintf "%s/%s" (url.TrimEnd('/')) (resource.TrimStart('/')))
    uri.AbsoluteUri

/// <summary>
/// Adds few default headers to the WEB API request.
/// </summary>
/// <param name="request">An HTTP Client request object.</param>
let withDefaultSettings (request : Request) = 
    request
    |> Request.keepAlive false
    |> Request.responseCharacterEncoding Encoding.UTF8
    |> Request.autoDecompression DecompressionScheme.GZip
    |> Request.setHeader (Accept "application/json")
    |> Request.setHeader (UserAgent DefaultUserAgent)

let withFormBody (bodyContent : Dictionary<string, string>) (request : Request) = 
    let formData = 
        bodyContent 
        |> Seq.map (fun keyValuePair -> sprintf "%s=%s" (WebUtility.UrlEncode(keyValuePair.Key)) (WebUtility.UrlEncode(keyValuePair.Value)))
    let formEndodedString = String.Join("&", formData)
    request
    |> Request.setHeader formUrlEncodedContentTypeHeader
    |> Request.bodyStringEncoded formEndodedString (Encoding.UTF8)

let withJsonBody bodyContent (request : Request) = 
    let body = 
        match box bodyContent with
        | :? String as s -> s
        | _ -> JsonConvert.SerializeObject bodyContent
    request
    |> Request.setHeader jsonContentTypeHeader
    |> Request.bodyStringEncoded body (Encoding.UTF8)

let getFrom uri = Request.createUrl Get uri |> withDefaultSettings
let postTo uri = Request.createUrl Post uri |> withDefaultSettings
let delete uri = Request.createUrl Delete uri |> withDefaultSettings
let putTo uri = Request.createUrl Put uri |> withDefaultSettings

let getResponse request =
    request
    |> HttpFs.Client.getResponse
    |> Hopac.Hopac.run

let getBody response =
    response
    |> Response.readBodyAsString
    |> Hopac.Hopac.run

let getPageCookies() = browser.Manage().Cookies

/// <summary>
/// Obtains the Request-Verification-Token from page body
/// </summary>
let getBodyRqVerifToken() = 
    let rqToken = element "//input[@name='__RequestVerificationToken']"
    rqToken.GetAttribute("value")

/// <summary>
/// Obtains the Request-Verification-Token from page cookies
/// </summary>
let getCookieRqVerificationToken() =
    getPageCookies().GetCookieNamed("__RequestVerificationToken")

/// <summary>
/// Obtains the login cookie from page cookies
/// </summary>
let getLoginCookie() =
    getPageCookies().GetCookieNamed(LoginCookieName)

/// <summary>
/// Sends an API request to clrear the cache.
/// </summary>
/// <param name="doLogin">A boolean value indicationg whethr to login as HOST or not.</param>
/// <remarks>
/// Must be logged in as HOST if not asking to login.<br />
/// Must have a paeg loaded in memory to obtain the COOKIES from.
/// </remarks>
let clearCache doLogin = 
    if doLogin then loginAsHost()

    let getExpiry (expiry : Nullable<DateTime>) =
        if expiry.HasValue then DateTimeOffset(expiry.Value) else DateTimeOffset.Now.AddMinutes(1.)

    let rvtokenHeader = getBodyRqVerifToken();

    let rvtokenCookie =
        let c = getCookieRqVerificationToken()
        let exp = getExpiry c.Expiry
        Cookie.create(c.Name, c.Name, exp, c.Path, c.Domain)

    let loginCookie =
        let c = getLoginCookie()
        let exp = getExpiry c.Expiry
        Cookie.create(c.Name, c.Name, exp, c.Path, c.Domain)

    let request = 
            postTo (root + "/DesktopModules/internalservices/API/controlbar/ClearHostCache")
            |> Request.setHeader (Custom("RequestVerificationToken", rvtokenHeader))
            |> Request.cookie rvtokenCookie
            |> Request.cookie loginCookie
            |> withJsonBody ""
    use response = request |> getResponse
    if response.statusCode >= 400 then
        failwithf "Clear cache reposne error. HTTP Code = %d" response.statusCode
    sleep 0.1
    waitPageLoad()
