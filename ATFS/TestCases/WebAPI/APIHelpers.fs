// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
// Copyright (c) 2015 - 2017, DNN Corporation
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

module APIHelpers

open System
open System.IO
open System.Text
open FSharp.Data
open FSharp.Data.JsonExtensions
open HttpFs.Client
open canopy
open DnnWebApi
open DnnUserLogin
open APIData

let mutable private createCnt = 0
let mutable SiteID = 0

let mutable private apiUrl = if useChildPortal then config.Site.SiteAlias+"/"+config.Site.ChildSitePrefix else config.Site.SiteAlias
let mutable private apiPortalId = 0
    //if useChildPortal then config.Site.SiteAlias+"/"+config.Site.ChildSitePrefix else config.Site.SiteAlias

let private bingportalPrefix =
    if useChildPortal then "C" else ""

let setAPIURL() =
    if useChildPortal then
        apiUrl <- config.Site.SiteAlias+"/"+config.Site.ChildSitePrefix
    else
        apiUrl <- config.Site.SiteAlias
    //bingportalPrefix <- childSitePrefix
    apiUrl

let private loginPage = "/Login"
let mutable private lastLoggedinUser = ""

// logs in and returns true if successful; false oherwise
let public doLoginBing (user : DnnUser) isPopup =
    let login u p =
        if u <> lastLoggedinUser then
            logOff()
            goto ("/")
            printfn "  Current URL: %A" browser.Url
            if isPopup then clickDnnPopupLink siteSettings.loginLinkId
            else goto loginPage
            loginUserNameTextBoxId << u
            loginPasswordTextBoxId << p
            press enter //click loginButtonId
            waitPageLoad()
            printfn "  Loggged in as %A" u
            closeToastNotification()
        let success = existsAndVisible siteSettings.loggedinUserImageLinkId
        if success then lastLoggedinUser <- u
        success

    match user with
    | Host ->
        let r = login hostUsername defaultPassword
        dismissWelcomePopup()
        r
    | RegisteredUser(u, p) -> login u p

/// <summary>ries to convert an item to an instance of another.</summary>
/// <param name="convertible">An objec to try casting into another</param>
/// <returns>An Option of the converted object (Some or None)</returns>
/// <remarks>This can be used by tests to validate whether returned values are assignable from one type to another.</remarks>
let convertTo<'a> convertible =
    match box convertible with
    | :? 'a -> Some convertible
    | _ -> None

let LogTurnOn = false
//let logFilePath = @"D:\Dnn.Automation\DNN.FSharp.Framework\ATFS\TestCases\WebAPI\Logs\" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt"
let logFilePath = FramerowkLogFilesFolder + @"\" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt"

let getRequestVerificationToken (needLogIn:bool, withLog:bool) =
    if needLogIn then
        logOff()
        loginAsHost()

    goto "/"
    let rvTokenDiv = element "//input[@name='__RequestVerificationToken']"
    let rvToken = rvTokenDiv.GetAttribute("value")
    rvToken

// Depends on the return value from apiTabsGetPortalTabs
let getPageIDList (pageJson:JsonValue) =
    let rtnPages = pageJson.AsArray()
    let tabIDArray = rtnPages |> Array.map (fun oneTabOnly -> oneTabOnly?TabId.AsString())
    tabIDArray

let private organizeParametersByEliminateEmpty (searchText) (filter) (pageIndex) =
    let mutable parameterString = ""
    let mutable parameterCnt = 0
    parameterString <- parameterString + "?searchtext=" + searchText.ToString()
    if filter = "" then
        parameterString <- parameterString + "&filter=0"
    else
        parameterString <- parameterString + "&filter=" +  filter.ToString()
    if pageIndex = 0 then
        parameterString <- parameterString + "&pageIndex=0"
    else
        parameterString <- parameterString + "&pageIndex=" +  pageIndex.ToString()
    parameterString

// Introduced in on Apr.05.2017, it will gradually replace most of the API calls.
let private forAllGetAPIs (loginInfo:UserLoginInfo, domainURL:string, actionURL:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + domainURL + actionURL)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private forAllPostAPIs (loginInfo:UserLoginInfo, domainURL:string, actionURL:string, postString:string, tabID:string, withLog:bool) =
    let myDefaultTabID =
        match tabID with
        | "" | "0" -> ""
        | _ -> tabID

    let mutable myTabID : NameValuePair = {Name = "TabId"; Value = myDefaultTabID }

    // To deal with couple special contentTypeValue
    let contentType =
        match actionURL with
        | "AddModule" -> formUrlEncodedContentTypeHeader
        | _ -> jsonContentTypeHeader

    let siteURL = setAPIURL()
    let request =
            postTo ("http://" + siteURL + domainURL + actionURL)
            |> Request.setHeader contentType
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.setHeader (customHeader myTabID)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString postString

    let response = request |> getResponse
    response

let commandLineGetAPIs (loginInfo:UserLoginInfo, pathString:string, withLog:bool) =
    let mutable siteURL = setAPIURL()

    let request =
            getFrom ("http://" + siteURL + "/API/PersonaBar/Command/" + pathString)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
    let response = request |> getResponse
    response

let commandLinePostAPIs (loginInfo:UserLoginInfo, pathString:string, cmdLine:string, withLog:bool) =
    let mutable siteURL = setAPIURL()

    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/Command/" + pathString)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString cmdLine
    let response = request |> getResponse
    response

let apiGetCmdList (loginInfo:UserLoginInfo, portalID:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable pathString = "List"
    let response = commandLineGetAPIs (loginInfo, pathString, withLog)
    response

let apiCmdLine (loginInfo:UserLoginInfo, portalID:string, cmdLine:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable pathString = "Cmd"
    let response = commandLinePostAPIs (loginInfo, pathString, cmdLine, withLog)
    response

// Soft Delete
let apiSetTabSoftDeleted (loginInfo:UserLoginInfo, tabID:string, withLog:bool) =
    let mutable myTabID : NameValuePair = {Name = "TabId"; Value = tabID }
    let mutable siteURL = setAPIURL()
    let mutable myPostString = """{"id":idReplaceMe}"""
    myPostString <- myPostString.Replace("idReplaceMe", tabID)
    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/Pages/DeletePage")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.setHeader (customHeader myTabID)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString
    let response = request |> getResponse
    response

// Set Permission to make it visible to All Users
let apiSetTabVisible (loginInfo:UserLoginInfo, tabID:string, withLog:bool) =
    let mutable myTabID : NameValuePair = {Name = "TabId"; Value = tabID }
    let mutable siteURL = setAPIURL()
    let mutable myPageDetails : JsonValue = JsonValue.Null
    let myURI = "/API/PersonaBar/Pages/"
    let request =
            getFrom ("http://"+apiUrl+myURI+"GetPageDetails?pageId="+tabID)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let responsePageDetails = request |> getResponse
    if responsePageDetails.statusCode = 200 then
        myPageDetails <- JsonValue.Parse(responsePageDetails |> getBody)
        let myDetailString = myPageDetails.ToString()
        let oldString = """"permissions": []""" //"""\"roleId\": -1,\r\n        \"roleName\": \"All Users\",\r\n        \"permissions\": []"""
        let newString = """"permissions": [{"permissionId":3,"permissionName":"View Tab","fullControl":false,"view":true,"allowAccess":true}]""" //"""\"roleId\": -1,\r\n        \"roleName\": \"All Users\",\r\n        \"permissions\": [\r\n          {\r\n            \"permissionId\": 3,\r\n            \"permissionName\": \"View Tab\",\r\n            \"fullControl\": false,\r\n            \"view\": true,\r\n            \"allowAccess\": true\r\n          }\r\n        ]"""
        let myPostString = myDetailString.Replace (oldString, newString)
        //Console.WriteLine mystring

        let request =
                postTo ("http://"+apiUrl+myURI+"SavePageDetails")
                |> Request.setHeader jsonContentTypeHeader
                |> Request.setHeader (customHeader loginInfo.RVToken)
                |> Request.setHeader (customHeader myTabID)
                |> Request.cookie (customCookie loginInfo.DNNCookie)
                |> Request.cookie (customCookie loginInfo.RVCookie)
                |> Request.bodyString myPostString
        request |> getResponse |> ignore
    responsePageDetails

//////////////////////////////////Dnn.PersonaBar.Users Starts//////////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Users+-+Request+Examples#Dnn.PersonaBar.Users-RequestExamples-GetUsers//
/////////////////////////////////////////////////////////////////////////////////////////////
let private usersGetAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    //GetProviders
    //GetSettings

    let mutable siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/Users/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private usersPostAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    //myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let mutable siteURL = setAPIURL()
    printfn "APIURL is: %A" siteURL
    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/Users/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString postString

    let response = request |> getResponse
    //printfn "User Creation request is: %A" request
    response

let apiGetUserAny(loginInfo:UserLoginInfo, searchText:string, filter:string, pageIndex:int, withLog:bool) =
    //GetUsers?searchText=&filter=0&pageIndex=0&pageSize=10&sortColumn=&sortAscending=false&searchText=amarjit
    let organizedParameters = organizeParametersByEliminateEmpty (searchText) (filter) (pageIndex) + "&sortColumn=&sortAscending=false&pageSize=10"
    let response = usersGetAPIs(loginInfo, "GetUsers" + organizedParameters, withLog)
    response

let apiGetUserDetail(loginInfo:UserLoginInfo, userID:string, withLog:bool) =
    let response = usersGetAPIs(loginInfo, "GetUserDetail?userid=" + userID, withLog)
    response

let apiGetUserFilters(loginInfo:UserLoginInfo, withLog:bool) =
    let response = usersGetAPIs(loginInfo, "GetUserFilters", withLog)
    response

let apiGetUserSuggestRoles(loginInfo:UserLoginInfo, keyword:string, withLog:bool) =
    let myKeyword =
        match keyword with
            | "" -> ""
            | _ -> keyword
    let response = usersGetAPIs(loginInfo, "GetSuggestRoles?keyword=" + myKeyword + "&count=10", withLog)
    response

let apiUpdateUserBasicInfo (loginInfo:UserLoginInfo, modifiedUserInfo:APICreateUserInfo, withLog:bool) =
    let mutable myRequestString = """{
                                        "userId": userIDReplaceMe,
                                        "displayName": "displayNameReplaceMe",
                                        "userName": "userNameReplaceMe",
                                        "email": "emailReplaceMe"
                                    }"""
    myRequestString <- myRequestString.Replace ("userIDReplaceMe", modifiedUserInfo.UserID)
    myRequestString <- myRequestString.Replace ("displayNameReplaceMe", modifiedUserInfo.DisplayName )
    myRequestString <- myRequestString.Replace ("userNameReplaceMe", modifiedUserInfo.UserName )
    myRequestString <- myRequestString.Replace ("emailReplaceMe", modifiedUserInfo.EmailAddress  )
    let response = usersPostAPIs(loginInfo, "UpdateUserBasicInfo", myRequestString, withLog)
    response

let apiChangeUserPassword (loginInfo:UserLoginInfo, userID:string, newPassword:string, withLog:bool) =
    let mutable myRequestString = """{
                                        "userId": userIDReplaceMe,
                                        "password": "passwordReplaceMe"
                                    }"""
    let myNewPassword =
        match newPassword with
            | "" -> "dnnhost"
            | _ -> newPassword
    myRequestString <- myRequestString.Replace ("userIDReplaceMe", userID)
    myRequestString <- myRequestString.Replace ("passwordReplaceMe", myNewPassword )
    let response = usersPostAPIs(loginInfo, "ChangePassword", myRequestString, withLog)
    response

let apiForceChangeUserPassword (loginInfo:UserLoginInfo, userID:string, withLog:bool) =
    let response = usersPostAPIs(loginInfo, "ForceChangePassword?userId="+userID, "", withLog)
    response

let apiSendPasswordResetLink (loginInfo:UserLoginInfo, userID:string, withLog:bool) =
    let response = usersPostAPIs(loginInfo, "SendPasswordResetLink?userId="+userID, "", withLog)
    response

let apiUpdateAuthorizeStatus (loginInfo:UserLoginInfo, userID:string, authorized:string, withLog:bool) =
    let myAuthorized =
        match authorized with
            | "" -> "true"
            | _ -> authorized
    let response = usersPostAPIs(loginInfo, "UpdateAuthorizeStatus?userId="+userID+"&authorized="+myAuthorized, "", withLog)
    response

let apiSoftDeleteUser (loginInfo:UserLoginInfo, userID:string, withLog:bool) =
    let response = usersPostAPIs(loginInfo, "SoftDeleteUser?userId="+userID, "", withLog)
    response

let apiUpdateSuperUserStatus (loginInfo:UserLoginInfo, userID:string, setStatus:string, withLog:bool) =
    let mySuperUserStatus =
        match setStatus with
            | "" -> "true"
            | _ -> setStatus
    let response = usersPostAPIs(loginInfo, "UpdateSuperUserStatus?userId="+userID+"&setSuperUser="+mySuperUserStatus, "", withLog)
    response

let apiRestoreDeletedUser (loginInfo:UserLoginInfo, userID:string, withLog:bool) =
    let response = usersPostAPIs(loginInfo, "RestoreDeletedUser?userId="+userID, "", withLog)
    response

let apiHardDeleteUser (loginInfo:UserLoginInfo, userID:string, withLog:bool) =
    let response = usersPostAPIs(loginInfo, "HardDeleteUser?userId="+userID, "", withLog)
    response

let apiCreateUser (loginInfo:UserLoginInfo, createUserInfo:APICreateUserInfo, withLog:bool) =
    //let mutable myRequestString = """{"userName": "userNameReplaceMe", "password": "passwordReplaceMe", "email": "emailReplaceMe", "firstName": "firstNameReplaceMe", "lastName": "lastNameReplaceMe"}"""
    let mutable myRequestString = """{"userName": "userNameReplaceMe", "password": "passwordReplaceMe", "email": "emailReplaceMe", "firstName": "firstNameReplaceMe", "lastName": "lastNameReplaceMe", "randomPassword":false,"authorize":authorizeReplaceMe,"notify":false}"""
    myRequestString <- myRequestString.Replace ("userNameReplaceMe", createUserInfo.UserName)
    myRequestString <- myRequestString.Replace ("passwordReplaceMe", createUserInfo.Password )
    myRequestString <- myRequestString.Replace ("emailReplaceMe", createUserInfo.EmailAddress)
    myRequestString <- myRequestString.Replace ("firstNameReplaceMe", createUserInfo.FirstName)
    myRequestString <- myRequestString.Replace ("lastNameReplaceMe", createUserInfo.LastName )
    myRequestString <- myRequestString.Replace ("authorizeReplaceMe", createUserInfo.Authorize )
    let response = usersPostAPIs(loginInfo, "CreateUser", myRequestString, withLog)
    response

let apiCreateUserAny (loginInfo:UserLoginInfo, createUserInfo:APICreateUserInfo, isDeleted:bool, authorized:bool, withLog:bool) =
    let isAuthorizedReplaceMe = if authorized then "true" else "false"
    let isDeletedReplaceMe = if isDeleted then "true" else "false"
    let mutable myRequestString = """{"userName":"userNameReplaceMe","password":"passwordReplaceMe","email":"emailReplaceMe","firstName":"firstNameReplaceMe","lastName":"lastNameReplaceMe","randomPassword":false,"authorize":authorizeReplaceMe,"notify":false,"isDeleted":isDeletedReplaceMe}"""

    myRequestString <- myRequestString.Replace ("userNameReplaceMe", createUserInfo.UserName)
    myRequestString <- myRequestString.Replace ("passwordReplaceMe", createUserInfo.Password )
    myRequestString <- myRequestString.Replace ("emailReplaceMe", createUserInfo.EmailAddress)
    myRequestString <- myRequestString.Replace ("firstNameReplaceMe", createUserInfo.FirstName)
    myRequestString <- myRequestString.Replace ("lastNameReplaceMe", createUserInfo.LastName )
    myRequestString <- myRequestString.Replace ("authorizeReplaceMe", isAuthorizedReplaceMe )
    myRequestString <- myRequestString.Replace ("isDeletedReplaceMe", isDeletedReplaceMe )
    let response = usersPostAPIs(loginInfo, "CreateUser", myRequestString, withLog)
    response

let apiCreateUsersBatch (loginInfo:UserLoginInfo, userNamePrefix:string, userCount:int, withLog:bool) =
    let randonStr = Guid.NewGuid().ToString
    let mutable myRequestString = ""
    let mutable userCreated = 0
    for i in 1..userCount do
        let userName = userNamePrefix + i.ToString()
        myRequestString <- """{"userName": "userNameReplaceMe", "password": config.Site.DefaultPassword, "email": "emailReplaceMe", "firstName": "firstNameReplaceMe", "lastName": "lastNameReplaceMe"}"""
        myRequestString <- myRequestString.Replace ("userNameReplaceMe", userNamePrefix + i.ToString())
        myRequestString <- myRequestString.Replace ("emailReplaceMe", userName + "@dnntest.com")
        myRequestString <- myRequestString.Replace ("firstNameReplaceMe", userName)
        myRequestString <- myRequestString.Replace ("lastNameReplaceMe", "DnnAuto")

        let request =
                postTo ("http://" + apiUrl + "/DesktopModules/personaBar/API/Users/CreateUser")
                |> Request.setHeader jsonContentTypeHeader
                |> Request.setHeader (customHeader loginInfo.RVToken)
                |> Request.cookie (customCookie loginInfo.DNNCookie)
                |> Request.cookie (customCookie loginInfo.RVCookie)
                |> Request.bodyString myRequestString
        use response = request |> getResponse

        if response.statusCode <> 200 then
            let requestLength = myRequestString.Length

            userCreated <- userCreated + 1

    userCreated

let apiLoginPopupAs (user : DnnUser) = doLoginBing user true
let apiUsersIfUserExists (loginInfo:UserLoginInfo, userName) =
    use response = apiGetUserAny (loginInfo, userName, "", 0, true)
    if response.statusCode = 200 then
        let samples = JsonValue.Parse(response |> getBody)
        let resultCount = samples.GetProperty("TotalResults").AsInteger()
        let resultRcd = samples.GetProperty("Results").AsArray()
        if resultCount >= 1 && resultRcd.GetUpperBound(0) >=0 then
            let userId = samples.GetProperty("Results").[0].GetProperty("userId").AsInteger()
            userId
        else
            (-1)
    else
        (-1)

//////////////////////////////////Dnn.PersonaBar.Roles Starts////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Roles+-+Request+Examples///
/////////////////////////////////////////////////////////////////////////////////////////////
let private rolesPostAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    let mutable siteURL = setAPIURL()
    //if useChildPortal then siteURL <- siteURL + "/" + config.Site.ChildSitePrefix

    let request =
            postTo ("http://" + siteURL + "/DesktopModules/PersonaBar/API/Roles/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    response

let private rolesGetAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/PersonaBar/api/Roles/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    response

let apiRolesGetRoles(loginInfo:UserLoginInfo, withLog:bool) =
    let response = rolesGetAPIs (loginInfo, "GetRoles?groupId=-2&keyword=&startIndex=0&pageSize=9999&reload=true", withLog)
    response

let apiRolesSaveRole(loginInfo:UserLoginInfo, roleNamePrefix:string, roleNameID:int, withLog:bool) =
    let mutable myPostString = """{
                                  "id": -1,
                                  "name": "RoleNameReplaceMe",
                                  "groupId": -1,
                                  "description": "Created by automation",
                                  "securityMode": 0,
                                  "status": 0,
                                  "isPublic": false,
                                  "autoAssign": false,
                                  "isSystem": false
                                }"""

    let myRoleNamePrefix =
        match roleNamePrefix with
        | "" -> "Role"
        | _ -> roleNamePrefix
    let myRoleNameStr =
        match roleNameID with
        | 0 -> myRoleNamePrefix + Guid.NewGuid().ToString()
        | _ -> myRoleNamePrefix + roleNameID.ToString()
    myPostString <- myPostString.Replace("RoleNameReplaceMe", myRoleNameStr)
    let response = rolesPostAPIs (loginInfo, "SaveRole?assignExistUsers=false", myPostString, withLog)
    response

let apiRolesSaveRoleMore(loginInfo:UserLoginInfo, roleInfo:APICreateRoleInfo, withLog:bool) =
    let mutable myPostString = """{
                                  "id": -1,
                                  "name": "RoleNameReplaceMe",
                                  "groupId": groupIdReplaceMe,
                                  "description": "Created by automation",
                                  "securityMode": securityModeReplaceMe,
                                  "status": statusReplaceMe,
                                  "isPublic": isPublicReplaceMe,
                                  "autoAssign": autoAssignReplaceMe,
                                  "isSystem": isSystemReplaceMe
                                }"""

    let myRoleNameStr =
        match roleInfo.Name with
        | "" -> "Role"
        | _ -> roleInfo.Name
    myPostString <- myPostString.Replace("RoleNameReplaceMe", myRoleNameStr)
    myPostString <- myPostString.Replace("groupIdReplaceMe", roleInfo.GroupId )
    myPostString <- myPostString.Replace("securityModeReplaceMe", roleInfo.SecurityMode )
    myPostString <- myPostString.Replace("statusReplaceMe", roleInfo.Status)
    myPostString <- myPostString.Replace("isPublicReplaceMe", roleInfo.IsPublic )
    myPostString <- myPostString.Replace("autoAssignReplaceMe", roleInfo.AutoAssign )
    myPostString <- myPostString.Replace("isSystemReplaceMe", roleInfo.IsSystem)

    let response = rolesPostAPIs (loginInfo, "SaveRole?assignExistUsers=false", myPostString, withLog)
    response

let apiRolesCreateNewRoleAny(loginInfo:UserLoginInfo, groupId:string, status:string, withLog:bool) =
    let randonStr = Guid.NewGuid().ToString()
    let myGroupId = match groupId with
                        | "" -> "-1"
                        | _ -> groupId
    let myStatus = match status with
                        | "" -> "1"
                        | _ -> status
    let mutable postString = SamplePostRolesCreation
    postString <- postString.Replace ("roleNameReplaceMe", "Role"+randonStr)
    postString <- postString.Replace ("roleDescirptionReplaceMe", "Role"+randonStr)
    postString <- postString.Replace ("groupIdReplaceMe", myGroupId)
    postString <- postString.Replace ("statusReplaceMe", myStatus)
    // DesktopModules/PersonaBar/API/Roles/AddUserToRole
    let response = rolesPostAPIs(loginInfo, "SaveRole?assignExistUsers=false", postString, true)
    response

let apiRolesCreateNewRole (loginInfo:UserLoginInfo, withLog:bool) =
    //"""{"id":-1,"name":"roleNameReplaceMe","groupId":-1,"description":"roleDescirptionReplaceMe","securityMode":0,"status":1,"isPublic":true,"autoAssign":false,"isSystem":false}"""
    let randonStr = Guid.NewGuid().ToString()
    let mutable postString = SamplePostRolesCreation
    postString <- postString.Replace ("roleNameReplaceMe", "Role"+randonStr)
    postString <- postString.Replace ("roleDescirptionReplaceMe", "Role"+randonStr)
    postString <- postString.Replace ("groupIdReplaceMe", "-1")
    postString <- postString.Replace ("statusReplaceMe", "1")
    // DesktopModules/PersonaBar/API/Roles/AddUserToRole
    let response = rolesPostAPIs(loginInfo, "SaveRole?assignExistUsers=false", postString, true)
    response

let apiRolesCreateNewRoles (loginInfo:UserLoginInfo, quantity:int, withLog:bool) =
    //"""{"id":-1,"name":"roleNameReplaceMe","groupId":-1,"description":"roleDescirptionReplaceMe","securityMode":0,"status":1,"isPublic":true,"autoAssign":false,"isSystem":false}"""
    for i in 1..quantity do
       use response = apiRolesCreateNewRole(loginInfo, withLog)
       let samples = JsonValue.Parse(response |> getBody)
       let rtnValue = samples.GetProperty("id").AsInteger()
       printfn "response.statusCode is %A\n" rtnValue

let getRoleIDByRoleName (loginInfo:UserLoginInfo, roleName:APIRoleName, withLog:bool) =
    use response = rolesGetAPIs(loginInfo, "GetRoles?groupId=-100&keyword=&startIndex=0&pageSize=100&reload=true", withLog)
    let arrayRoles = JsonValue.Parse(response |> getBody).GetProperty("roles").AsArray()
    let newArray = arrayRoles
                        |> Array.filter (fun myElem -> myElem.GetProperty("name").AsString().ToUpper().Replace(" ", "") = roleName.ToString())
    let rtnRoleId =
        if newArray.Length > 0 then newArray.[0].GetProperty("id").AsString() else "-1"
    rtnRoleId

let apiRolesAddUserToRole (loginInfo:UserLoginInfo, userID:string, roleName:APIRoleName, withLog:bool) =
    let randonStr = Guid.NewGuid().ToString()
    let mutable roleID = "-1"
    roleID <- getRoleIDByRoleName (loginInfo, roleName, false)
    //SamplePostRolesAddUserToRole = """{"userId":userIDReplaceMe,"roleId":roleIDReplaceMe,"isAdd":true}"""
    //let mutable postString = """{"userId":userIDReplaceMe,"roleId":roleIDReplaceMe,"isAdd":true}"""  //SamplePostRolesAddUserToRole
    let mutable postString = """{"userId":userIDReplaceMe, "roleId":roleIDReplaceMe,"startTime":"2016-08-31T16:00:00.000Z","expiresTime":"0001-01-01T00:00:00"}"""  //SamplePostRolesAddUserToRole
    //{"userId":8,"displayName":"ADMINISTRATORS DnnTester","roleId":0,"startTime":"0001-01-01T00:00:00","expiresTime":"0001-01-01T00:00:00","allowExpired":true,"allowDelete":true}
    postString <- postString.Replace ("userIDReplaceMe", userID)
    postString <- postString.Replace ("roleIDReplaceMe", roleID)
    let response = rolesPostAPIs(loginInfo, "AddUserToRole?notifyUser=true&isOwner=false", postString, true)
    response

// Currently we don't have API for login, as a workaround, have to login from UI
let apiLogin (userName:string) (password:string) =
    let ru = RegisteredUser(userName, password) //Organize by data type
    let rtn = apiLoginPopupAs ru
    let allCookieString = browser.Manage().Cookies.AllCookies
    let userCookie = browser.Manage().Cookies.GetCookieNamed(".DOTNETNUKE")
    let rvCookie = browser.Manage().Cookies.GetCookieNamed("__RequestVerificationToken")
    let myRVToken = getRequestVerificationToken(false, true)

    // if userName = config.Site.HostUserName then
    //     myHostLoginInfo <- myLocalLoginInfo
    { UserName = userName
      Password = password
      DisplayName = userName
      DNNCookie = { Name=userCookie.Name; Value=userCookie.Value }
      RVCookie = { Name=rvCookie.Name; Value=rvCookie.Value }
      RVToken = { Name="RequestVerificationToken"; Value=myRVToken}
    }
    // Default Key Testing Accounts for automation: AutoAdmin, AutoCM, AutoCE, AutoCOM, etc...; password all use default one: dnnhost
    // Except for default Host account

let apiLoginAsHost () =
    if defaultHostLoginInfo.UserName = "" then
        let loginInfo = apiLogin config.Site.HostUserName config.Site.DefaultPassword
        defaultHostLoginInfo <- loginInfo
    defaultHostLoginInfo

let rec apiLoginDefaultAccountWithCreation2 (roleName:APIRoleName, portalName:string) =
    logOff()
    let mutable userName = ""
    // Assign Default Account UserName based on RoleName
    let mutable userNamePrefix = ""
    let userNamePrefix = portalName
    match roleName  with
        | APIRoleName.HOSTUSER  -> userName <- "host"
        | APIRoleName.ADMINISTRATORS -> userName <- userNamePrefix+"AutoADMIN"
        | _ -> userName <- userNamePrefix+"AutoTesterRU"

    goto ("/"+bingportalPrefix)
    // Try to log in by using the default account
    let ru = RegisteredUser(userName, config.Site.DefaultPassword) //Organize by data type
    printfn "  Try to Log in As a User %A" userName
    let rtn = apiLoginPopupAs ru

    // If log in failed, it means we need to create a new one first
    if not rtn && createCnt <= 2 then
        createCnt <- createCnt + 1
        // Log in as Host to be the "creator"
        let hostUser = apiLoginAsHost ()
        let newUserInfo : APICreateUserInfo =
            {
                FirstName = userName
                LastName = "DnnTester"
                UserName = userName
                Password = config.Site.DefaultPassword
                EmailAddress = userName + "DnnTester@dnntest.com"
                DisplayName = userName
                UserID = "0"
                Authorize = "true"
            }
        let createdUser = apiCreateUser (hostUser, newUserInfo, true)
        let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
        let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
        apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
        printfn "  Created New User %A" newUserInfo.UserName
        apiLoginDefaultAccountWithCreation2 (roleName,portalName)  |> ignore
        printfn "  Log in As New User %A" newUserInfo.UserName

    let allCookieString = browser.Manage().Cookies.AllCookies
    let userCookie = browser.Manage().Cookies.GetCookieNamed(".DOTNETNUKE")
    let rvCookie = browser.Manage().Cookies.GetCookieNamed("__RequestVerificationToken")
    let myRVToken = getRequestVerificationToken(false, true)

    { UserName = userName
      Password = config.Site.DefaultPassword
      DisplayName = userName //This value Needs to change later.
      DNNCookie = { Name=userCookie.Name; Value=userCookie.Value }
      RVCookie = { Name=rvCookie.Name; Value=rvCookie.Value }
      RVToken = { Name="RequestVerificationToken"; Value=myRVToken }
    }

let rec apiLoginDefaultAccountWithCreation (roleName:APIRoleName) =
    logOff()
    let mutable userName = ""

    // Assign Default Account UserName based on RoleName
    let mutable userNamePrefix = ""
    if useChildPortal then userNamePrefix <- "C"
    match roleName  with
        | APIRoleName.HOSTUSER  -> userName <- "host"
        | APIRoleName.ADMINISTRATORS -> userName <- userNamePrefix+"AutoADMIN"
        | _ -> userName <- userNamePrefix+"AutoTesterRU"

    // Try to log in by using the default account
    let ru = RegisteredUser(userName, config.Site.DefaultPassword) //Organize by data type
    printfn "  Try to Log in As a User %A" userName
    let rtn = apiLoginPopupAs ru
    printfn "  If user login successful or not %A" rtn
    // If log in failed, it means we need to create a new one first
    if not rtn && createCnt <= 2 then
        createCnt <- createCnt + 1
        printfn "  This is %A time try to create a new user" createCnt
        // Log in as Host to be the "creator"
        let hostUser = apiLoginAsHost ()
        let newUserInfo : APICreateUserInfo =
            {
                FirstName = userName
                LastName = "DnnTester"
                UserName = userName
                Password = config.Site.DefaultPassword
                EmailAddress = userName + "DnnTester@dnntest.com"
                DisplayName = userName
                UserID = "0"
                Authorize = "true"
            }
        let createdUser = apiCreateUser (hostUser, newUserInfo, true)
        let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
        //let createdUserId = sampleUserCreated.GetProperty("Results").GetProperty("userId").AsString()
        let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
        apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
        printfn "  Created New User %A" newUserInfo.UserName
        apiLoginDefaultAccountWithCreation roleName |> ignore
        printfn "  Log in As New User %A" newUserInfo.UserName

    let allCookieString = browser.Manage().Cookies.AllCookies
    let userCookie = browser.Manage().Cookies.GetCookieNamed(".DOTNETNUKE")
    let rvCookie = browser.Manage().Cookies.GetCookieNamed("__RequestVerificationToken")
    let myRVToken = getRequestVerificationToken(false, true)
    { UserName = userName
      Password = config.Site.DefaultPassword
      DisplayName = userName //This value Needs to change later.
      DNNCookie = { Name=userCookie.Name; Value=userCookie.Value }
      RVCookie = { Name=rvCookie.Name; Value=rvCookie.Value }
      RVToken = { Name="RequestVerificationToken"; Value=myRVToken}
    }

let apiLoginAsAdmin2 (portalName:string) =
    createCnt <- 0
    let userNamePrefix = bingportalPrefix
    if defaultAdminLoginInfo.UserName <> userNamePrefix+"AutoADMIN" then
        defaultAdminLoginInfo <- apiLoginDefaultAccountWithCreation2( APIRoleName.ADMINISTRATORS, portalName)
    defaultAdminLoginInfo

let apiLoginAsAdmin () =
    createCnt <- 0
    let userNamePrefix = if useChildPortal then "C" else ""
    if defaultAdminLoginInfo.UserName <> userNamePrefix+"AutoADMIN" then
        defaultAdminLoginInfo <- apiLoginDefaultAccountWithCreation APIRoleName.ADMINISTRATORS
    defaultAdminLoginInfo

let apiLoginAsRU () =
    createCnt <- 0
    let userNamePrefix = if useChildPortal then "C" else ""
    if defaultRULoginInfo.UserName <> userNamePrefix+"AutoTesterRU" then
        defaultRULoginInfo <- apiLoginDefaultAccountWithCreation APIRoleName.REGISTEREDUSERS
    defaultRULoginInfo

let BVTLogInDataPreparation (roleName:APIRoleName) =
    canopy.configuration.skipRemainingTestsInContextOnFailure <- true
    let userNamePrefix = if useChildPortal then "C" else ""
    let mutable myLocalUserLoginInfo : UserLoginInfo = {
                                                            UserName = ""
                                                            Password = ""
                                                            DisplayName = ""
                                                            DNNCookie = { Name=""; Value="" }
                                                            RVCookie = { Name=""; Value="" }
                                                            RVToken = { Name=""; Value="" }
                                                        }
    match roleName  with
        | APIRoleName.ADMINISTRATORS ->
                myLocalUserLoginInfo <- apiLoginAsAdmin()
        | APIRoleName.HOSTUSER ->
                myLocalUserLoginInfo <- apiLoginAsHost()
        | APIRoleName.ANONYMOUS ->
                //logOff()
                //let allCookieString = browser.Manage().Cookies.AllCookies
                //let userCookie = browser.Manage().Cookies.GetCookieNamed(".DOTNETNUKE")
                //let RVCookie = browser.Manage().Cookies.GetCookieNamed("__RequestVerificationToken")
                //let myRVToken = getRequestVerificationToken(false, true)
                myLocalUserLoginInfo <- {
                    UserName = ""
                    Password = ""
                    DisplayName = ""
                    DNNCookie = { Name=".DOTNETNUKE"; Value="" }
                    RVCookie = { Name="__RequestVerificationToken"; Value="" }
                    RVToken = { Name="__RequestVerificationToken"; Value="" }
                    }
        | _ ->
                myLocalUserLoginInfo <- apiLoginAsRU()

    canopy.configuration.skipRemainingTestsInContextOnFailure <- false
    myLocalUserLoginInfo

let postRunSQLQuery (loginInfo:UserLoginInfo, sqlString:string, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let mutable myRequestString = """{"connection":"SiteSqlServer","query":"sqlToReplaceMe"}"""
    myRequestString <- myRequestString.Replace ("sqlToReplaceMe", sqlString)
    //for c:OpenQA.Selenium.Cookie in allCookies do
      //  cc.Add (new Cookie(c.Name, c.Value))
    let request =
            postTo ("http://" + siteURL + "/DesktopModules/personaBar/API/SqlConsole/RunQuery")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myRequestString
    let response = request |> getResponse
    //printfn "response is %A" response
    let requestLength = myRequestString.Length
    //printfn "\r\nRESPONSE =>\r\n%A\r\n" response
    //if expectedResponseCode <> 0 then
    //    test <@ response.statusCode = responseCode @>
    response

// queryName could be empty. It will generate random name.
let PostSaveSQLQuery (loginInfo:UserLoginInfo, sqlString:string, queryName:string, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let mutable myQueryName = queryName
    if myQueryName = "" then myQueryName <- "Q" + Guid.NewGuid().ToString()
    let mutable myRequestString = """{"id":-1,"name":"queryNameReplaceMe","query":"sqlToReplaceMe","connection":"SiteSqlServer"}"""
    myRequestString <- myRequestString.Replace ("sqlToReplaceMe", sqlString)
    myRequestString <- myRequestString.Replace ("queryNameReplaceMe", myQueryName)

    let request =
            postTo ("http://" + siteURL + "/DesktopModules/personaBar/API/SqlConsole/SaveQuery")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myRequestString
    let response = request |> getResponse
    response

let GetSavedSQLQuery (loginInfo:UserLoginInfo, withLog:bool) =
    let mutable siteURL = setAPIURL()

    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/SqlConsole/GetSavedQueries")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)

    let response = request |> getResponse
    response

let GetSavedSQLQueryById (loginInfo:UserLoginInfo, myQueryId:string, withLog:bool) =
    let mutable siteURL = setAPIURL()

    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/SqlConsole/GetSavedQuery?id=" + myQueryId)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)

    let response = request |> getResponse
    response

let DeleteSQLQueryById (loginInfo:UserLoginInfo, myQueryId:string, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let myRequestString = """{id : """ + myQueryId + """}"""

    let request =
            postTo ("http://" + siteURL + "/DesktopModules/personaBar/API/SqlConsole/DeleteQuery")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myRequestString

    let response = request |> getResponse
    response

let getHostCSSEditor (loginInfo:UserLoginInfo, withLog:bool) =
    let mutable siteURL = setAPIURL()

    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/CssEditor/GetStyleSheet?portalId=" + SiteID.ToString())
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    response

let postHostCSSEditor (loginInfo:UserLoginInfo, myPortalId:int, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let mutable myPostString = SamplePostStyleSheet
    myPostString <- myPostString.Replace("portalIdReplaceMe", myPortalId.ToString())

    let request =
            postTo ("http://" + siteURL + "/API/personaBar/CssEditor/UpdateStyleSheet")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString
    let response = request |> getResponse
    response

let restoreHostCSSEditor (loginInfo:UserLoginInfo, myPortalId:int, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let mutable myPostString = SamplePostDefaultStyleSheet
    myPostString <- myPostString.Replace("portalIdReplaceMe", myPortalId.ToString())

    let request =
            postTo ("http://" + siteURL + "/DesktopModules/personaBar/API/CssEditor/RestoreStyleSheet?portalId=" + myPortalId.ToString())
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString
    let response = request |> getResponse
    response

let getConfigFileList (loginInfo:UserLoginInfo, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/ConfigConsole/GetConfigFilesList")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let getConfigFileByName (loginInfo:UserLoginInfo, fileName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/ConfigConsole/GetConfigFile?fileName=" + fileName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let updateConfigFileByName (loginInfo:UserLoginInfo, fileName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let mutable myPostString = SamplePostUpdateConfigFile
    myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let request =
            postTo ("http://" + siteURL + "/DesktopModules/personaBar/API/ConfigConsole/UpdateConfigFile")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    //printfn "response is %A" response
    response

let mergeConfigFileByName (loginInfo:UserLoginInfo, fileName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let mutable myPostString = SamplePostMergeConfigFile
    myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let request =
            postTo ("http://" + siteURL + "/DesktopModules/personaBar/API/ConfigConsole/MergeConfigFile")
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    //printfn "response is %A" response
    response

let getSiteMapAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    //GetProviders
    //GetSettings
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/API/PersonaBar/SEO/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let updateSiteMapAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let siteURL = setAPIURL()
    let mutable myPostString = postString
    //myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let request =
            postTo ("http://" + siteURL + "/DesktopModules/personaBar/API/Sitemap/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    //printfn "response is %A" response
    response

let getAdminLogsAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    //GetProviders
    //GetSettings
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/API/PersonaBar/AdminLogs/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let updateAdminLogsAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let siteURL = setAPIURL()
    let mutable myPostString = postString
    //myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/AdminLogs/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    //printfn "response is %A" response
    response

//////////////////////////////////DesktopModules/DNNCorp/ResourceLibrary/API Starts////////////////////////////////
//   API/PersonaBar/TaskScheduler/   ///
/////////////////////////////////////////////////////////////////////////////////////////////
let private schedulerGetAPIs (loginInfo:UserLoginInfo, apiName:string, queryString:string, withLog:bool) =
    let mutable myQueryString = apiName + queryString
    let response = forAllGetAPIs (loginInfo, "/API/PersonaBar/TaskScheduler/", myQueryString, withLog)
    response

let private schedulerPostAPIs (loginInfo:UserLoginInfo, apiName:string, postString:string, withLog:bool) =
    //let mutable myQueryString = apiName
    let response = forAllPostAPIs (loginInfo, "/API/PersonaBar/TaskScheduler/", apiName, postString, "", withLog)
    response

let getSchedulerAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    //GetProviders
    //GetSettings
    let response = schedulerGetAPIs (loginInfo, pathName, "", withLog)
    response

let apiGetScheduleItems (loginInfo:UserLoginInfo, queryString:string, withLog:bool) =
    let response = schedulerGetAPIs (loginInfo, "GetScheduleItems", queryString, withLog)
    response

let apiGetScheduleItemDetailById (loginInfo:UserLoginInfo, queryString:string, withLog:bool) =
    let response = schedulerGetAPIs (loginInfo, "GetScheduleItem", queryString, withLog)
    response

let apiFindScheduleItemByName (loginInfo:UserLoginInfo, scheduleName:string, withLog:bool) =
    use response = schedulerGetAPIs (loginInfo, "GetScheduleItems", "?", withLog)
    let samples = JsonValue.Parse(response |> getBody)
    let scheduleArray = samples.GetProperty("Results").AsArray()
    let myFindString = scheduleName.ToUpper().Replace(" ", "")
    let newArray = scheduleArray
                        |> Array.filter (fun myElem -> myElem.GetProperty("FriendlyName").AsString().ToUpper().Replace(" ", "") = myFindString)
    if newArray.GetUpperBound(0) >= 0 then
        let scheduleId = newArray.[0].GetProperty("ScheduleID").AsInteger()
        scheduleId
    else
        0

let apiPostRunSchedule (loginInfo:UserLoginInfo, scheduleId:string, withLog:bool) =
    let mutable myPostString = """{"ScheduleID":ScheduleIDReplaceMe,"FriendlyName":"FriendlyNameReplaceMe","TypeFullName":"TypeFullNameReplaceMe","Enabled":true,"ScheduleStartDate":"","TimeLapse":1,"TimeLapseMeasurement":"d","RetryTimeLapse":1,"RetryTimeLapseMeasurement":"h","RetainHistoryNum":100,"AttachToEvent":"","CatchUpEnabled":false,"ObjectDependencies":"","Servers":""}"""

    let response = apiGetScheduleItemDetailById(loginInfo, "?scheduleId="+scheduleId, false)
    let body = JsonValue.Parse(response |> getBody)

    let schduleDetails = body.GetProperty("Results")

    myPostString <- schduleDetails.ToString()

    use response = schedulerPostAPIs (loginInfo, "RunSchedule", myPostString, withLog)
    response

let getUIPortalsAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/API/PersonaBar/Portals/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let getPortalsAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/Sites/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let postPortalAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let siteURL = setAPIURL()
    let mutable myPostString = postString
    //myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let request =
            postTo ("http://" + siteURL + "/DesktopModules/personaBar/API/Sites/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    //printfn "response is %A" response
    response

let getVocabularyAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/Vocabularies/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let postVocabularyAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let siteURL = setAPIURL()
    let mutable myPostString = postString
    //myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/Vocabularies/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    //printfn "response is %A" response
    response

let postVocabularyCreateTerm (loginInfo:UserLoginInfo, vocabularyId:string, withLog:bool) =
    let mutable postString = SamplePostVocabularyTermCreation

    let myRandomStr = Guid.NewGuid().ToString()
    postString <- postString.Replace ("VocabularyIdReplaceMe", vocabularyId)
    postString <- postString.Replace ("TermNameReplaceMe", "Name-" + myRandomStr)
    postString <- postString.Replace ("TermDescriptionReplaceMe", "Description-" + myRandomStr)
    postString <- postString.Replace ("ParentTermIdReplaceMe", "-1")
    // Create Term
    let response2 = postVocabularyAPIs(loginInfo, "CreateTerm", postString, true)
    response2

let postVocabularyCreateDefault (loginInfo:UserLoginInfo) =
    let mutable postString = SamplePostVocabulary
    let myRandomStr = Guid.NewGuid().ToString()
    postString <- postString.Replace ("vocabularyNameReplaceMe", "Voc-"+myRandomStr)
    postString <- postString.Replace ("DescriptionReplaceMe", "Description Of " + "Voc-"+myRandomStr)
    postString <- postString.Replace ("ScopeTypeIdReplaceMe", "1")
    postString <- postString.Replace ("TypeIdReplaceMe", "1")

    // Create vocabulary
    let response = postVocabularyAPIs(loginInfo, "CreateVocabulary", postString, true)
    response

//////////////////////////////////Dnn.PersonaBar.portals Starts////////////////////////////////
//https://///
/////////////////////////////////////////////////////////////////////////////////////////////

let private sitesGetAPIs(loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    //let mutable siteURL = APIURL
    //if useChildPortal then siteURL <- siteURL + "/" + config.Site.ChildSitePrefix
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/personaBar/API/Sites/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private sitesPostAPIs(loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/Sites/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString postString

    let response = request |> getResponse
    //printfn "response is %A" response
    response

let apiSitesGetPortals(loginInfo:UserLoginInfo, withLog:bool) =
    let response = sitesGetAPIs (loginInfo, "GetPortals?portalGroupId=-1&filter=&pageIndex=0&pageSize=10", withLog)
    response

let apiSitesGetPortalAny(loginInfo:UserLoginInfo, filterString:string, withLog:bool) =
    let response = sitesGetAPIs (loginInfo, "GetPortals?portalGroupId=-1&filter=" + filterString + "&pageIndex=0&pageSize=10", withLog)
    response

let apiSitesGetPortalTabs(loginInfo:UserLoginInfo, withLog:bool) =
    let response = sitesGetAPIs (loginInfo, "GetPortalTabs?portalId=0&cultureCode=en-US&isMultiLanguage=false", withLog)
    response

let apiSitesGetPortalLocales(loginInfo:UserLoginInfo, withLog:bool) =
    let response = sitesGetAPIs (loginInfo, "GetPortalLocales?portalId=0", withLog)
    response

let setAPIPortalId() =
    if useChildPortal then
        printfn "current APIPortalId = %A" apiPortalId
        if apiPortalId = 0 then
            let response = apiSitesGetPortalAny(defaultHostLoginInfo, config.Site.ChildSitePrefix, false)
            if response.statusCode <= 201 then
                let body = JsonValue.Parse(response |> getBody)
                let portalId = body.GetProperty("Results").[0].GetProperty("PortalID").AsInteger()
                apiPortalId <- portalId
    else
        apiPortalId <- 0
    apiPortalId

let apiSitesGetRequiresQuestionAndAnswer(loginInfo:UserLoginInfo, withLog:bool) =
    let response = sitesGetAPIs (loginInfo, "RequiresQuestionAndAnswer", withLog)
    response

let apiSitesCreateChildPortal(loginInfo:UserLoginInfo, siteTemplateName:string, siteName:string, withLog:bool) =
    let myRandomStr = Guid.NewGuid().ToString()
    let mutable postString = SamplePostCreatePortal

    // Using Default Template for now
    let mySiteTemplateName =
        match siteTemplateName with
            | "" | "default" -> "Default Website.template|en-US|"
            | _ -> "Default Website.template|en-US|"
    let mySiteName =
        match siteName with
            | "" -> "Child" + myRandomStr
            | _ -> siteName
    let siteAlias = mySiteName
    let siteKeyWord = "Auto, DNN, Testing"
    postString <- postString.Replace ("SiteTemplateReplaceMe", mySiteTemplateName)
    postString <- postString.Replace ("SiteNameReplaceMe",mySiteName)
    postString <- postString.Replace ("SiteAliasReplaceMe", "http://"+apiUrl+"/"+siteAlias)
    postString <- postString.Replace ("SiteKeywordsReplaceMe", siteKeyWord)

    let response = sitesPostAPIs (loginInfo, "CreatePortal", postString, withLog)
    response

let createChildPortalwithWaiting(siteName:string) =
    let mutable foundChildPortal = false
    let timeWaitToRecreate = 60.0 // wait for xx seconds, then re-create
    let mutable timePassed = 0.0
    let mutable reTryLeft = 5
    let timeWaited = 5 // seconds
    defaultHostLoginInfo <- apiLoginAsHost()
    while reTryLeft > 0 do
        timePassed <- 0.0
        while not foundChildPortal && timePassed <= timeWaitToRecreate do
            printfn "Debug: Try to find out if childsite exist or not yet"
            let response = apiSitesGetPortalAny(defaultHostLoginInfo, siteName, false)
            if response.statusCode <= 201 then
                let body = JsonValue.Parse(response |> getBody)
                let totalResults = body.GetProperty("TotalResults").AsInteger()
                if totalResults > 0 then
                    let portalId = body.GetProperty("Results").[0].GetProperty("PortalID").AsInteger()
                    apiPortalId <- portalId
                    foundChildPortal <- true
                    reTryLeft <- 0
                    printfn "Debug: portalId found =  %A"  apiPortalId
                else
                    foundChildPortal <- false
            else
                foundChildPortal <- false
            sleep timeWaited //wait for xx second and try again.

        if reTryLeft > 0 then
            // Try to Create One
            let mutable postString = SamplePostCreatePortal
            // Using Default Template for now
            let mySiteTemplateName = "Default Website.template|en-US|"
            let siteKeyWord = "Auto, DNN, Testing"
            postString <- postString.Replace ("SiteTemplateReplaceMe", mySiteTemplateName)
            postString <- postString.Replace ("SiteNameReplaceMe",siteName)
            postString <- postString.Replace ("SiteAliasReplaceMe", "http://"+apiUrl+"/"+siteName)
            postString <- postString.Replace ("SiteKeywordsReplaceMe", siteKeyWord)
            printfn "Debug: Try to Create one after waiting"
            let responsePortal = sitesPostAPIs (defaultHostLoginInfo, "CreatePortal", postString, false)
            reTryLeft <- reTryLeft - 1
    apiPortalId

let apiSitesDeleteChildPortal(loginInfo:UserLoginInfo, siteID:string, withLog:bool) =
    let response = sitesPostAPIs (loginInfo, "DeletePortal?portalId="+siteID, "{}", withLog)
    response

let apiSitesDeleteExpiredPortals(loginInfo:UserLoginInfo, withLog:bool) =
    let response = sitesPostAPIs (loginInfo, "DeleteExpiredPortals", "", withLog)
    response

let apiSitesExportPortalTemplate(loginInfo:UserLoginInfo, portalID:string, tabResponse:Response, withLog:bool) =
    let myRandomStr = Guid.NewGuid().ToString()
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable postString = SamplePostExportPortalTemplate

    let samples = JsonValue.Parse(tabResponse |> getBody)
    let rtnArray = samples.GetProperty("Results").GetProperty("ChildTabs").AsArray()
    // Parse & re-organize the data for Export purpose. This is for "Pages" section
    // https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Sites+-+Request+examples
    let data = rtnArray |> Array.map (fun oneTab -> """{"TabId":""" + oneTab?TabId.AsString() + ""","ParentTabId":""" + oneTab?ParentTabId.AsString() + ""","CheckedState": "Checked"}""")
    let pagesStr = "[" + (data |> Seq.map string |> String.concat ",") + "]"

    postString <- postString.Replace ("FileNameReplaceMe", "Export-" + myRandomStr)
    postString <- postString.Replace ("PortalIdRepalceMe", myPortalID)
    postString <- postString.Replace ("PagesReplaceMe", pagesStr)

    let response = sitesPostAPIs (loginInfo, "ExportPortalTemplate", postString, withLog)
    response

//////////////////////////////////Dnn.PersonaBar.Tabs Starts////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Tabs+-+Request+examples///
/////////////////////////////////////////////////////////////////////////////////////////////
let private tabsGetAPIs(loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    //let mutable siteURL = APIURL
    //if useChildPortal then siteURL <- siteURL + "/" + config.Site.ChildSitePrefix
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/DesktopModules/PersonaBar/api/Tabs/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private tabsPostAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    let siteURL = setAPIURL()

    let request =
            postTo ("http://" + siteURL + "/DesktopModules/PersonaBar/api/Tabs/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    response

let private pagesPostAPIs_old (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    let siteURL = apiUrl

    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/Pages/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString

    let response = request |> getResponse
    response

let apiTabsGetPortalTabs(loginInfo:UserLoginInfo, portalID:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID

    let mutable pathString = """GetPortalTabs?portalId=""" + myPortalID + """&cultureCode=en-US&isMultiLanguage=false&excludeAdminTabs=true&roles=&disabledNotSelectable=false&sortOrder=0&selectedTabId=-1"""
    let response = tabsGetAPIs (loginInfo, pathString, withLog)
    response

let apiTabsGetTabsDescendants(loginInfo:UserLoginInfo, portalID:string, parentID:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID

    let myParentID =
        match parentID with
            | "" -> "42"
            | _ -> parentID
    let mutable pathString = """GetTabsDescendants?portalId=""" + myPortalID + """&cultureCode=en-US&isMultiLanguage=true&excludeAdminTabs=true&disabledNotSelectable=false&roles=&sortOrder=0&parentId=""" + myParentID
    let response = tabsGetAPIs (loginInfo, pathString, withLog)
    response

let apiTabsSavePageDetails(loginInfo:UserLoginInfo, pageName:string, withLog:bool) =
    let myPostString = """{"tabId":0,"name":"pageNameReplaceMe","status":"Visible","localizedName":"","alias":"","title":"pageNameReplaceMe","description":"pageDescriptionRepaceMe","keywords":"pageKeyWordsReplaceMe","tags":"pageTagsReplaceMe","url":"","externalRedirection":"","fileRedirection":"","existingTabRedirection":"","includeInMenu":true,"thumbnail":"","created":"","hierarchy":"","hasChild":false,"type":0,"customUrlEnabled":true,"pageType":"normal","isCopy":false,"trackLinks":false,"startDate":null,"endDate":null,"createdOnDate":"2016-11-10T18:50:27.188Z","placeholderURL":"/","modules":[],"permissions":{"permissionDefinitions":[{"permissionId":3,"permissionName":"View Tab","fullControl":false,"view":true,"allowAccess":false},{"permissionId":18,"permissionName":"Add","fullControl":false,"view":false,"allowAccess":false},{"permissionId":19,"permissionName":"Add Content","fullControl":false,"view":false,"allowAccess":false},{"permissionId":20,"permissionName":"Copy","fullControl":false,"view":false,"allowAccess":false},{"permissionId":21,"permissionName":"Delete","fullControl":false,"view":false,"allowAccess":false},{"permissionId":22,"permissionName":"Export","fullControl":false,"view":false,"allowAccess":false},{"permissionId":23,"permissionName":"Import","fullControl":false,"view":false,"allowAccess":false},{"permissionId":24,"permissionName":"Manage Settings","fullControl":false,"view":false,"allowAccess":false},{"permissionId":25,"permissionName":"Navigate","fullControl":false,"view":false,"allowAccess":false},{"permissionId":4,"permissionName":"Edit Tab","fullControl":true,"view":false,"allowAccess":false}],"rolePermissions":[{"roleId":0,"roleName":"Administrators","permissions":[],"locked":true,"default":true},{"roleId":5,"roleName":"Registered Users","permissions":[],"locked":false,"default":true},{"roleId":-1,"roleName":"All Users","permissions":[],"locked":false,"default":true},{"roleId":3,"roleName":"Content Managers","permissions":[],"locked":true,"default":true},{"roleId":2,"roleName":"Content Editors","permissions":[],"locked":true,"default":true}],"userPermissions":[]},"permanentRedirect":false,"linkNewWindow":false}"""

    let response = tabsPostAPIs (loginInfo, "SavePageDetails", myPostString, withLog)
    response

//////////////////////////////////Dnn.PersonaBar.Tabs Starts////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Pages+-+Request+examples///
/////////////////////////////////////////////////////////////////////////////////////////////
// Including:
// - GetDefaultSettings
// - SavePageDetails
// - GetPageList?searchKey=
let private pagesGetAPIs(loginInfo:UserLoginInfo, apiName:string, withLog:bool) =
    let mutable apiDomain = "Pages/"
    let response = forAllGetAPIs (loginInfo, "/api/PersonaBar/"+apiDomain, apiName, withLog)
    response

let private pagesPostAPIs (loginInfo:UserLoginInfo, apiName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    let mutable apiDomain = "Pages/"
    let response = forAllPostAPIs (loginInfo, "/api/PersonaBar/"+apiDomain, apiName, myPostString, "", withLog)
    response

let apiPagesGetPageList(loginInfo:UserLoginInfo, portalID:string, searchPageName:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable pathString = "GetPageList?searchKey=" + searchPageName
    let response = pagesGetAPIs (loginInfo, pathString, withLog)
    response

let apiPagesGetPageDetails(loginInfo:UserLoginInfo, portalID:string, pageID:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable pathString = "GetPageDetails?pageId=" + pageID
    let response = pagesGetAPIs (loginInfo, pathString, withLog)
    response

let apiPagesSearchPages (loginInfo:UserLoginInfo, portalID:string, searchString:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable pathString = "SearchPages?" + searchString
    let response = pagesGetAPIs (loginInfo, pathString, withLog)
    response

let apiPagesGetDefaultSettings(loginInfo:UserLoginInfo, portalID:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable pathString = "GetDefaultSettings?"
    let response = pagesGetAPIs (loginInfo, pathString, withLog)
    response

// Might be expired. by toggle User Mode
let apiPagesEditModeForPage(loginInfo:UserLoginInfo, portalID:string, pageID:string, withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable pathString = "EditModeForPage?id=" + pageID
    let response = pagesGetAPIs (loginInfo, pathString,  withLog)
    response

let apiPagesSavePageDetails(loginInfo:UserLoginInfo, portalID:string, pageName:string, withLog:bool) =
    defaultHostLoginInfo <- apiLoginAsHost()
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let myPageName =
        match pageName with
            | "" -> "Pg-" + Guid.NewGuid().ToString().Substring(1, 12)
            | _ -> pageName
    let mutable pathString = "SavePageDetails"
    let mutable myPostString = SamplePostPageDetailsTemplate
    use response = apiPagesGetDefaultSettings (defaultHostLoginInfo, portalID, withLog)

    if response.statusCode = 200 then
        let samples = JsonValue.Parse(response |> getBody)
        //printfn "PagesGetDefaultSettings: %A" samples
        let tabTemplatesReplaceMe = "[]"
        let permissionsReplaceMe = samples.GetProperty("permissions").ToString()
        let templatesReplaceMe = samples.GetProperty("templates").ToString()
        myPostString <- myPostString.Replace("tabTemplatesReplaceMe", tabTemplatesReplaceMe)
        myPostString <- myPostString.Replace("permissionsReplaceMe", permissionsReplaceMe)
        myPostString <- myPostString.Replace("templatesReplaceMe", templatesReplaceMe)
        myPostString <- myPostString.Replace("pageNameReplaceme", myPageName)
    let response = pagesPostAPIs (loginInfo, pathString, myPostString, withLog)
    response

//////////////////////////////////Dnn.PersonaBar.Template Starts////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Pages+-+Request+examples///
/////////////////////////////////////////////////////////////////////////////////////////////
// Including:
let private templatesGetAPIs(loginInfo:UserLoginInfo, apiName:string, withLog:bool) =
    let mutable apiDomain = "Templates/"
    let response = forAllGetAPIs (loginInfo, "/api/PersonaBar/"+apiDomain, apiName, withLog)
    response

let private templatesPostAPIs (loginInfo:UserLoginInfo, apiName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    let mutable apiDomain = "Templates/"
    let response = forAllPostAPIs (loginInfo, "/api/PersonaBar/"+apiDomain, apiName, myPostString, "", withLog)
    response

let apiTemplatesGetPageTemplates(loginInfo:UserLoginInfo, portalID:string,withLog:bool) =
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let mutable pathString = "GetPageTemplates?searchKey="
    let response = templatesGetAPIs (loginInfo, pathString, withLog)
    response

let apiTemplatesSavePageDetails(loginInfo:UserLoginInfo, portalID:string, pageName:string, templateId:string, withLog:bool) =
    defaultHostLoginInfo <- apiLoginAsHost()
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let myPageName =
        match pageName with
            | "" -> "Pg-" + Guid.NewGuid().ToString().Substring(1, 12)
            | _ -> pageName
    let mutable pathString = "SavePageDetails"
    let mutable myPostString = SamplePostTemmplatesSavePageDetailsTemplate
    myPostString <- myPostString.Replace("templateNameReplaceMe", myPageName)
    myPostString <- myPostString.Replace("templateIdReplaceMe", templateId)

    let response = templatesPostAPIs (loginInfo, pathString, myPostString, withLog)
    response

let apiTemplatesSavePagePermissions(loginInfo:UserLoginInfo, templateId:string, withLog:bool) =
    defaultHostLoginInfo <- apiLoginAsHost()

    let mutable pathString = "SavePagePermissions"
    let mutable myPostString = """{"rolePermissions":[],"userPermissions":[],"tabId":tabIdReplaceMe}"""
    myPostString <- myPostString.Replace("tabIdReplaceMe", templateId)

    let response = templatesPostAPIs (loginInfo, pathString, myPostString, withLog)
    response

let apiTemplatesEditModeForPage(loginInfo:UserLoginInfo, templateId:string, withLog:bool) =
    defaultHostLoginInfo <- apiLoginAsHost()

    let mutable pathString = "EditModeForPage?id=" + templateId
    let mutable myPostString = """{}"""
    myPostString <- myPostString.Replace("tabIdReplaceMe", templateId)
    let response = templatesPostAPIs (loginInfo, pathString, myPostString, withLog)
    response

let apiTemplatesDeletePage(loginInfo:UserLoginInfo, templateId:string, withLog:bool) =
    defaultHostLoginInfo <- apiLoginAsHost()

    let mutable pathString = "DeletePage"
    let mutable myPostString = """{"id": idReplaceMe}"""
    myPostString <- myPostString.Replace("idReplaceMe", templateId)

    let response = templatesPostAPIs (loginInfo, pathString, myPostString, withLog)
    response

// keyTagName, for instance: "workflowId":
// valueStr "" means any value
let public findAndReplace (sourceJsonString:JsonValue, keyTagName:string, valueStr:string, newValue:string) =
    let mutable mySourceString = sourceJsonString.ToString()
    let findKeyword = mySourceString.IndexOf (keyTagName, 1)
    if findKeyword > 0 then
        let findOldValueFrom = findKeyword+keyTagName.Length
        let findOldValueTo = mySourceString.IndexOf (",", findKeyword+keyTagName.Length) // Find the first "," after keyTagName
        let oldValue = mySourceString.Substring(findOldValueFrom, findOldValueTo-findOldValueFrom)
        let replaceString = keyTagName + oldValue
        let newString = keyTagName + newValue
        mySourceString <- mySourceString.Replace(replaceString, newString)
    mySourceString

//apiPagesUpdatePageDetails(loginInfo, "0", myTabID, "workflowId", newWorkFlow, withLog)
let public apiPagesUpdatePageDetails(loginInfo:UserLoginInfo, portalID:string, pageID:string, updateTagName:string, newValue:string, withLog:bool) =
    //defaultHostLoginInfo <- apiLoginAsHost()
    let myPortalID =
        match portalID with
            | "" -> "0"
            | _ -> portalID
    let pageDetailsResponse = apiPagesGetPageDetails(loginInfo, myPortalID, pageID, withLog)
    let pageDetailsJsonString = JsonValue.Parse(pageDetailsResponse |> getBody)
    //Commonly Replace for
    //1. "workflowId": 3,
    //2. "trackLinks": true,
    //3. "startDate": "2017-04-20T00:00:00",
    //4. "endDate": "2017-04-26T23:59:59",
    let lookingFor = "\"" + updateTagName + "\":"
    let updatedString = findAndReplace (pageDetailsJsonString, lookingFor, "", newValue)
    let mutable pathString = "SavePageDetails"
    let response = pagesPostAPIs (loginInfo, pathString, updatedString, withLog)
    response

let apiSetTabWorkflow (loginInfo:UserLoginInfo, tabID:string, newWorkflowValue:string, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let myResponse = apiPagesUpdatePageDetails(loginInfo, "0", tabID, "workflowId", newWorkflowValue, withLog)
    myResponse

let apiSetTabTrackLinks (loginInfo:UserLoginInfo, tabID:string, newTrackLinksValue:string, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let myResponse = apiPagesUpdatePageDetails(loginInfo, "0", tabID, "trackLinks", newTrackLinksValue, withLog)
    myResponse

let apiSetTabStartDate (loginInfo:UserLoginInfo, tabID:string, newStartDate:string, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let myResponse = apiPagesUpdatePageDetails(loginInfo, "0", tabID, "startDate", newStartDate, withLog)
    myResponse

let apiSetTabEndDate (loginInfo:UserLoginInfo, tabID:string, newEndDate:string, withLog:bool) =
    let mutable siteURL = setAPIURL()
    let myResponse = apiPagesUpdatePageDetails(loginInfo, "0", tabID, "endDate", newEndDate, withLog)
    myResponse

let apiSaveBulkPages(loginInfo:UserLoginInfo, bulkString:string, workflow:int, trackLinks:string, startDate:string, endDate:string, withPublish:bool, visible:bool, softDeleted:bool, withLog:bool) =
    let pagePrefixName = "Page" //loginInfo.UserName
    let mutable defaultPostString = """{"bulkPages":"Page1\n>Page1-1\n>>Page1-1-1\n>>Page1-1-2\n>>Page1-1-3\n>>Page1-1-4\n>>Page1-1-5\n>Page1-2\n>>Page1-2-1\n>>Page1-2-2\n>>Page1-2-3\n>>Page1-2-4\n>>Page1-2-5\n>Page1-3\n>>Page1-3-1\n>>Page1-3-2\n>>Page1-3-3\n>>Page1-3-4\n>>Page1-3-5\n>Page1-4\n>>Page1-4-1\n>>Page1-4-2\n>>Page1-4-3\n>>Page1-4-4\n>>Page1-4-5\n>Page1-5\n>>Page1-5-1\n>>Page1-5-2\n>>Page1-5-3\n>>Page1-5-4\n>>Page1-5-5\nPage2\n>Page2-1\n>>Page2-1-1\nPage3\n>Page3-1\n>>Page3-1-1\nPage4\n>Page4-1\n>>Page4-1-1\nPage5\n>Page5-1\n>>Page5-1-1","parentId":"-1","keywords":"","tags":"","includeInMenu":true,"startDate":null,"endDate":null}"""

    let myPostString =
        match bulkString with
            | "" -> defaultPostString
            | _ -> bulkString

    let response = pagesPostAPIs_old (loginInfo, "SaveBulkPages", myPostString, withLog)
    if response.statusCode = 200 then
        let rtnPages = JsonValue.Parse(response |> getBody).GetProperty("Response").GetProperty("pages").AsArray()

        let tabIDArray = rtnPages |> Array.map (fun oneTabOnly -> oneTabOnly?tabId.AsString())
        //let pagesStr = "[" + (dataStrArray |> Seq.map string |> String.concat ",") + "]"
        for i in 0..tabIDArray.GetUpperBound(0) do
            if tabIDArray.[i].AsInteger() > 0 then
                if visible then
                    let responseVisible = apiSetTabVisible (loginInfo, tabIDArray.[i], true)
                    if responseVisible.statusCode = 200 then Console.WriteLine ("PageID:"+tabIDArray.[i]+" is Visible")
                if softDeleted then
                    let responseDeleted = apiSetTabSoftDeleted (loginInfo, tabIDArray.[i], true)
                    if responseDeleted.statusCode = 200 then Console.WriteLine ("PageID:"+tabIDArray.[i]+" is Soft Deleted")
                if workflow > 1 then
                    let responseDeleted = apiSetTabWorkflow (loginInfo, tabIDArray.[i], workflow.ToString(), true)
                    if responseDeleted.statusCode = 200 then Console.WriteLine ("PageID:"+tabIDArray.[i]+" is Soft Deleted")
                if startDate <> "" then
                    let responseStartDate = apiSetTabStartDate (loginInfo, tabIDArray.[i], startDate, true)
                    if responseStartDate.statusCode = 200 then Console.WriteLine ("PageID:"+tabIDArray.[i]+" set StartDate")
                if endDate <> "" then
                    let responseEndDate = apiSetTabEndDate (loginInfo, tabIDArray.[i], startDate, true)
                    if responseEndDate.statusCode = 200 then Console.WriteLine ("PageID:"+tabIDArray.[i]+" set EndDate")
                if trackLinks <> "" then
                    let responseTrackLinks = apiSetTabTrackLinks (loginInfo, tabIDArray.[i], trackLinks, true)
                    if responseTrackLinks.statusCode = 200 then Console.WriteLine ("PageID:"+tabIDArray.[i]+" set trackLinks")
    response

//InternalService
let private isGetAPIs (loginInfo:UserLoginInfo, apiName:string, queryString:string, withLog:bool) =
    let mutable myQueryString = apiName + queryString
    let response = forAllGetAPIs (loginInfo, "/API/internalservices/controlBar/", myQueryString, withLog)
    response

let private isPostAPIs (loginInfo:UserLoginInfo, apiName:string, postString:string, tabID:string, withLog:bool) =
    let mutable myPostString = postString
    let response = forAllPostAPIs (loginInfo, "/API/internalservices/controlBar/", apiName, myPostString, tabID, withLog)
    response

let apiISToggleUserMode(loginInfo:UserLoginInfo, pageId:string, mode:string, withLog:bool) =
    let myMode =
        match mode with
            | "Edit" | "edit" -> "EDIT"
            | _ -> "VIEW"
    let mutable apiNameStr = "ToggleUserMode"
    let mutable myPostString = "{\"UserMode\":\"" + myMode + "\"}\""
    let response = isPostAPIs (loginInfo, apiNameStr, myPostString, pageId, withLog)
    response

let apiISGetPortalDesktopModulesAll(loginInfo:UserLoginInfo, withLog:bool) =
    //GetPortalDesktopModules?category=All&loadingStartIndex=0&loadingPageSize=10&searchTerm=&excludeCategories=&sortBookmarks=true&topModule=None
    let mutable apiNameStr = "GetPortalDesktopModules"
    let mutable myQueryString = "?category=All&loadingStartIndex=0&loadingPageSize=100&searchTerm=&excludeCategories=&sortBookmarks=true&topModule=None"
    let response = isGetAPIs (loginInfo, apiNameStr, myQueryString, withLog)
    response

let apiISGetPortalDesktopModulesAny(loginInfo:UserLoginInfo, searchTerm:string, withLog:bool) =
    //GetPortalDesktopModules?category=All&loadingStartIndex=0&loadingPageSize=10&searchTerm=&excludeCategories=&sortBookmarks=true&topModule=None
    let mutable apiNameStr = "GetPortalDesktopModules"
    let mutable myQueryString = "?category=All&loadingStartIndex=0&loadingPageSize=100&searchTerm=" + searchTerm+ "&excludeCategories=&sortBookmarks=true&topModule=None"
    let response = isGetAPIs (loginInfo, apiNameStr, myQueryString, withLog)
    response

let apiISPostAddModule(loginInfo:UserLoginInfo, pageId:string, moduleId:string, withLog:bool) =
    //GetPortalDesktopModules?category=All&loadingStartIndex=0&loadingPageSize=10&searchTerm=&excludeCategories=&sortBookmarks=true&topModule=None
    let mutable apiNameStr = "AddModule"
    let mutable myPostString = "Visibility=0&Position=-1&Module="+moduleId+"&Pane=ContentPane&AddExistingModule=false&CopyModule=false&Sort=-1"
    let response = isPostAPIs (loginInfo, apiNameStr, myPostString, pageId, withLog)
    response

let apiCreatePortal(loginInfo:UserLoginInfo, portalName:string, withLog:bool) =
    let myPortalNamePrefix =
        match portalName with
            | "" -> "child" + Guid.NewGuid().ToString()
            | _ -> portalName

    let myPortalName = portalName
    let response = apiSitesCreateChildPortal(loginInfo, "", myPortalName, withLog)
    if response.statusCode = 200 then
        let samples = JsonValue.Parse(response |> getBody)
        let portalID = samples.GetProperty("Portal").GetProperty("PortalID").AsInteger()
        Console.WriteLine ("portalID: "+portalID.ToString())
        sleep 30 // Bing claims that this is needed. I bet he can find a better way than waiting this long.
    response

let apiCreateBulkPortals(loginInfo:UserLoginInfo, portalNamePrefix:string, quantity:int, withLog:bool) =
    let myPortalNamePrefix =
        match portalNamePrefix with
            | "" -> "child"
            | _ -> portalNamePrefix

    let portalIDArray = Array.create quantity 0
    for i in 1..quantity do
        let myPortalName = myPortalNamePrefix + i.ToString()
        let response = apiSitesCreateChildPortal(loginInfo, "", myPortalName, withLog)
        if response.statusCode = 200 then
            let samples = JsonValue.Parse(response |> getBody)
            let portalID = samples.GetProperty("Portal").GetProperty("PortalID").AsInteger()
            portalIDArray.[i-1] <- portalID
            Console.WriteLine ("portalID: "+portalID.ToString())
            sleep 30 // Bing claims that this is needed. I bet he can find a better way than waiting this long.
    portalIDArray

let private SiteInfoGetAPIs(loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    //let mutable siteURL = APIURL
    //if useChildPortal then siteURL <- siteURL + "/" + config.Site.ChildSitePrefix
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/API/PersonaBar/SiteSettings/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private SiteInfoPostAPIs(loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/SiteSettings/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString postString
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let apiSiteInfoGetPortalSettings(loginInfo:UserLoginInfo, portalId:string, cultureCode:string, withLog:bool) =
    let myPortalID =
        match portalId with
            | "" -> SiteID.ToString()
            | _ -> portalId
    let myCultureCode =
        match cultureCode with
            | "" -> "en-US"
            | _ -> cultureCode
    let response = SiteInfoGetAPIs (loginInfo, "GetPortalSettings?portalId="+myPortalID+"&cultureCode="+myCultureCode, withLog)
    response

let apiSiteInfoUpdatePortalSettings(loginInfo:UserLoginInfo, postString:string, withLog:bool) =
    let response = SiteInfoPostAPIs (loginInfo, "UpdatePortalSettings", postString, withLog)
    response

//////////////////////////////////Dnn.PersonaBar.Themes Starts//////////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Themes+-+Request+Examples//
/////////////////////////////////////////////////////////////////////////////////////////////

let private themesGetAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    //GetProviders
    //GetSettings
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/api/PersonaBar/Themes/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private themesPostAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    //myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let siteURL = setAPIURL()
    //if useChildPortal then siteURL <- siteURL + "/" + config.Site.ChildSitePrefix

    let request =
        postTo ("http://" + siteURL + "/DesktopModules/PersonaBar/api/Themes/" + pathName)
        |> Request.setHeader jsonContentTypeHeader
        |> Request.setHeader (customHeader loginInfo.RVToken)
        |> Request.cookie (customCookie loginInfo.DNNCookie)
        |> Request.cookie (customCookie loginInfo.RVCookie)
        |> Request.bodyString myPostString

    let response = request |> getResponse
    //printfn "response is %A" response
    response

let apiThemesGetThemes(loginInfo:UserLoginInfo, withLog:bool) =
    let response = themesGetAPIs (loginInfo, "GetThemes?level=3", withLog)
    response

let apiThemesGetCurrentTheme(loginInfo:UserLoginInfo, withLog:bool) =
    let response = themesGetAPIs (loginInfo, "GetCurrentTheme?language=en-US", withLog)
    response

let apiThemesGetEditableTokens(loginInfo:UserLoginInfo, withLog:bool) =
    let response = themesGetAPIs (loginInfo, "GetEditableTokens", withLog)
    response

let apiThemesGetEditableSettings(loginInfo:UserLoginInfo, tokenSettingsName:string, withLog:bool) =
    let myTokenSettingsName =
        match tokenSettingsName with
            | "" -> "Admin%2FContainers%2FActionButton.ascx" // 0 - Skin; 1 - Container
            | _ -> tokenSettingsName

    let response = themesGetAPIs (loginInfo, "GetEditableSettings?token=" + myTokenSettingsName + "", withLog)
    response

let apiThemesGetEditableValues(loginInfo:UserLoginInfo, tokenSettingsName:string, valueName:string, withLog:bool) =
    let myTokenSettingsName =
        match tokenSettingsName with
            | "" -> "Admin%2FContainers%2FActionButton.ascx" // 0 - Skin; 1 - Container
            | _ -> tokenSettingsName

    let myValueName =
        match valueName with
            | "" -> "DisplayLink" // 0 - Skin; 1 - Container
            | _ -> valueName

    let response = themesGetAPIs (loginInfo, "GetEditableValues?token=" + myTokenSettingsName + "&setting=" + myValueName, withLog)
    response

let apiThemesGetThemeFiles(loginInfo:UserLoginInfo, themeName:string, withLog:bool) =
    // Level = 4 means Global??
    let response = themesGetAPIs (loginInfo, "GetThemeFiles?themeName=" + themeName + "&type=0&level=4", withLog)
    response

let apiThemesParseTheme(loginInfo:UserLoginInfo, parseType:string, themeName:string, withLog:bool) =
    let myParseType =
        match parseType with
            | "" -> "0" // 0 - Skin; 1 - Container
            | _ -> parseType

    let myThemeName =
        match themeName with
            | "" -> "Xcillion"
            | _ -> themeName

    let mutable myPostString = """{"themeName":"themeNameReplaceMe","parseType":parseTypeReplaceMe}"""

    myPostString <- myPostString.Replace ("parseTypeReplaceMe", myParseType)
    myPostString <- myPostString.Replace ("themeNameReplaceMe", myThemeName)
    let response = themesPostAPIs (loginInfo, "ParseTheme", myPostString, withLog)
    response

let apiThemesDeleteTheme(loginInfo:UserLoginInfo, parseType:string, themeName:string, withLog:bool) =
    //{name: "Home", type: 0, path: "[G]/Cavalier/Home.ascx" }
    let myParseType =
        match parseType with
            | "" -> "0" // 0 - Skin; 1 - Container
            | _ -> parseType

    let myThemeName =
        match themeName with
            | "" -> "Xcillion"
            | _ -> themeName

    let mutable myPostString = """{"themeName":"themeNameReplaceMe","parseType":parseTypeReplaceMe}"""

    myPostString <- myPostString.Replace ("parseTypeReplaceMe", myParseType)
    myPostString <- myPostString.Replace ("themeNameReplaceMe", myThemeName)
    let response = themesPostAPIs (loginInfo, "ParseTheme", myPostString, withLog)
    response

let apiThemesApplyDefaultTheme(loginInfo:UserLoginInfo, themeName:string, withLog:bool) =
    let myThemeName =
        match themeName with
            | "" -> "Xcillion"
            | _ -> themeName
    let mutable myPostString = """{"themeName":"themeNameReplaceMe", "level":4}"""
    myPostString <- myPostString.Replace ("themeNameReplaceMe", myThemeName)
    let response = themesPostAPIs (loginInfo, "ApplyDefaultTheme?language=en-US", myPostString, withLog)
    response

let apiThemesApplyTheme(loginInfo:UserLoginInfo, themeScope:string, themeFiles:JsonValue[], themeName:string, withLog:bool) =
    //{scope:1,themeFile:{canDelete:false,name:"popupskin",path:"[G]skins/cavalier/popupskin",themeName:"Cavalier",thumbnail:null,type:0}}
    let mutable myPostString = """{scope:themeScopeReplaceMe,themeFile:themeFileReplaceMe}"""

    let myThemeScope =
        match themeScope with
            | "" -> "1" // 1 - Site Skin  2 - Edit Skin  3 - Site & Edit Skin
            | _ -> themeScope

    myPostString <- myPostString.Replace("themeScopeReplaceMe", myThemeScope)

    let myThemeName =
        match themeName with
            | "" -> "Home"
            | _ -> themeName

    let mutable myThemeFile = ""
    for i in 0..themeFiles.GetUpperBound(0) do
        if themeFiles.[i].GetProperty("name").AsString() = themeName then
            myThemeFile <- themeFiles.[i].ToString()
    myPostString <- myPostString.Replace("themeFileReplaceMe", myThemeFile)
    let response = themesPostAPIs (loginInfo, "ApplyTheme?language=en-US", myPostString, withLog)
    response

///////////////////////////////////// Dnn.PersonaBar.Seo /////////////////////////////////////////
//////https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Seo+-+Request+examples//////
//////////////////////////////////////////////////////////////////////////////////////////////////
let private seoGetAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/API/PersonaBar/SEO/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    response

let private seoPostAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =

    let mutable myPostString = postString
    //myPostString <- myPostString.Replace("FileNameReplaceMe", fileName)
    let siteURL = setAPIURL()
    //if useChildPortal then siteURL <- siteURL + "/" + config.Site.ChildSitePrefix

    let request =
        postTo ("http://" + siteURL + "/API/PersonaBar/SEO/" + pathName)
        |> Request.setHeader jsonContentTypeHeader
        |> Request.setHeader (customHeader loginInfo.RVToken)
        |> Request.cookie (customCookie loginInfo.DNNCookie)
        |> Request.cookie (customCookie loginInfo.RVCookie)
        |> Request.bodyString myPostString

    let response = request |> getResponse
    response

let apiSeoGetGeneralSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let response = seoGetAPIs (loginInfo, "GetGeneralSettings", withLog)
    response

let apiSeoUpdateGeneralSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let mutable myPostString = """{
                                   "EnableSystemGeneratedUrls":true,
                                   "ReplaceSpaceWith":"-",
                                   "ForceLowerCase":false,
                                   "AutoAsciiConvert":false,
                                   "ForcePortalDefaultLanguage":true,
                                   "DeletedTabHandlingType":"Do404Error",
                                   "RedirectUnfriendly":true,
                                   "RedirectWrongCase":false
                                }"""
    let response = seoPostAPIs (loginInfo, "UpdateGeneralSettings", myPostString, withLog)
    response

let apiSeoGetRegexSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let response = seoGetAPIs (loginInfo, "GetRegexSettings", withLog)
    response

let apiSeoUpdateRegexSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let mutable myPostString = """{
                                   "IgnoreRegex":"(?<!linkclick\\.aspx.+)(?:(?<!\\?.+)(\\.pdf$|\\.gif$|\\.png($|\\?)|\\.css($|\\?)|\\.js($|\\?)|\\.jpg$|\\.axd($|\\?)|\\.swf$|\\.flv$|\\.ico$|\\.xml($|\\?)|\\.txt$))",
                                   "DoNotRewriteRegex":"/DesktopModules/|/Providers/|/LinkClick\\.aspx|/profilepic\\.ashx|/DnnImageHandler\\.ashx|/__browserLink/|/API/",
                                   "UseSiteUrlsRegex":"/rss\\.aspx|Telerik.RadUploadProgressHandler\\.ashx|BannerClickThrough\\.aspx|(?:/[^/]+)*/Tabid/\\d+/.*default\\.aspx",
                                   "DoNotRedirectRegex":"(\\.axd)|/Rss\\.aspx|/SiteMap\\.aspx|\\.ashx|/LinkClick\\.aspx|/Providers/|/DesktopModules/|ctl=MobilePreview|/ctl/MobilePreview|/API/",
                                   "DoNotRedirectSecureRegex":"",
                                   "ForceLowerCaseRegex":"",
                                   "NoFriendlyUrlRegex":"/Rss\\.aspx",
                                   "DoNotIncludeInPathRegex":"/nomo/\\d+|/runningDefault/[^/]+|/popup/(?:true|false)|/(?:page|category|sort|tags)/[^/]+|tou/[^/]+|(/utm[^/]+/[^/]+)+",
                                   "ValidExtensionlessUrlsRegex":"\\.asmx/|\\.ashx/|\\.svc/|\\.aspx/|\\.axd/",
                                   "RegexMatch":"[^\\w\\d _-]"
                                }"""
    let response = seoPostAPIs (loginInfo, "UpdateRegexSettings", myPostString, withLog)
    response

let apiSeoGetSitemapSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let response = seoGetAPIs (loginInfo, "GetSitemapSettings", withLog)
    response

let apiSeoUpdateSitemapSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let mutable myPostString = """
                                {
                                   "SitemapUrl":"http://dnnce.lvh.me/SiteMap.aspx",
                                   "SitemapLevelMode":false,
                                   "SitemapMinPriority":0.1,
                                   "SitemapIncludeHidden":false,
                                   "SitemapExcludePriority":0.3,
                                   "SitemapCacheDays":0
                                }"""
    let response = seoPostAPIs (loginInfo, "UpdateSitemapSettings", myPostString, withLog)
    response

// Expired from 901
let apiSeoGetProviders(loginInfo:UserLoginInfo, withLog:bool) =
    let response = seoGetAPIs (loginInfo, "GetProviders", withLog)
    response

let apiSeoGetExtensionUrlProviders(loginInfo:UserLoginInfo, withLog:bool) =
    let response = seoGetAPIs (loginInfo, "GetExtensionUrlProviders", withLog)
    response

let apiSeoGetSiteMapProviders(loginInfo:UserLoginInfo, withLog:bool) =
    let response = seoGetAPIs (loginInfo, "GetSiteMapProviders", withLog)
    response

let apiSeoUpdateSiteMapProviders(loginInfo:UserLoginInfo, withLog:bool) =
    let mutable myPostString = """{
                                   "Name":"coreSitemapProvider",
                                   "Enabled":true,
                                   "Priority":0.8,
                                   "OverridePriority":false
                                }"""
    let response = seoPostAPIs (loginInfo, "UpdateSitemapProvider", myPostString, withLog)
    response

let apiSeoUpdateProviders(loginInfo:UserLoginInfo, withLog:bool) =
    let mutable myPostString = """{
                                   "Name":"coreSitemapProvider",
                                   "Enabled":true,
                                   "Priority":0.8,
                                   "OverridePriority":false
                                }"""
    let response = seoPostAPIs (loginInfo, "UpdateProvider", myPostString, withLog)
    response

let apiSeoSitemapResetCache(loginInfo:UserLoginInfo, withLog:bool) =
    let response = seoPostAPIs (loginInfo, "ResetCache", "", withLog)
    response

let apiSeoCreateVerification(loginInfo:UserLoginInfo, withLog:bool) =
    let response = seoPostAPIs (loginInfo, "CreateVerification?verification=sample.html", "", withLog)
    response

let apiSeoTestURL(loginInfo:UserLoginInfo, withLog:bool) =
    let myURIString = "TestUrlRewrite?uri=httep://" + config.Site.SiteAlias + "/Home"
    let response = seoGetAPIs (loginInfo, myURIString, withLog)
    response

let apiSeoTestURLRewriter(loginInfo:UserLoginInfo, withLog:bool) =
    let myURIString = "TestUrlRewrite?uri=http://" + config.Site.SiteAlias + "/Home"
    let response = seoGetAPIs (loginInfo, myURIString, withLog)
    response

//////////////////////////////////Dnn.PersonaBar.Extensions Starts////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Extensions+-+Request+examples///
/////////////////////////////////////////////////////////////////////////////////////////////
let private extensionsPostAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    let siteURL = setAPIURL()

    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/Extensions/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString
    let response = request |> getResponse
    response

// This is a special API call for Extention with file upload as form data.
let private extensionsPostFormAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    let siteURL = setAPIURL()

    let contentType = ContentType( ContentType.create("multipart", "form-data", Encoding.UTF8, "-------------------------acebdf13572468") )
    let request =
            postTo ("http://" + siteURL + "/API/PersonaBar/Extensions/" + pathName)
            |> Request.setHeader contentType
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.setHeader (AcceptLanguage "en-US,en;q=0.8")
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString
    //System.Net.Http.ByteArrayContent
    let response = request |> getResponse
    response

let private extensionsGetAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + "/API/PersonaBar/Extensions/" + pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    response

let apiGetExtensionsAvailablePackages (loginInfo:UserLoginInfo, extType:string, withLog:bool) =
    let myPathName = "GetAvailablePackages?packageType="+extType
    let response = extensionsGetAPIs (loginInfo, myPathName, withLog)
    response

let apiGetExtensionsInstalledPackages (loginInfo:UserLoginInfo, extType:string, withLog:bool) =
    let myPathName = "GetInstalledPackages?packageType="+extType
    let response = extensionsGetAPIs (loginInfo, myPathName, withLog)
    response

let apiExtensionsParsePackage(loginInfo:UserLoginInfo, withLog:bool) =
    let mutable myPostString = """---------------------------acebdf13572468
Content-Disposition: form-data; name="POSTFILE"; filename="DnnSharp.RedirectToolkit-2.2.0-Install.zip"
Content-Type: application/x-zip-compressed

FileBinaryReplaceMe
---------------------------acebdf13572468--"""
    //<@INCLUDE *D:\DnnSharp.RedirectToolkit-2.2.0-Install.zip*@>
    //file name: d:\
    let fileName = @"d:\DnnSharp.RedirectToolkit-2.2.0-Install.zip"
    //let reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
    let array = File.ReadAllBytes(fileName)
    //let fileString = File.ReadAllText(fileName)
    //let fileLength = fileString.Length
    //let fileString = reader.BaseStream.Read()
    //printfn "here is the data: %A" reader
    let fileString = Encoding.Default.GetString(array, 0, array.Length)
    myPostString <- myPostString.Replace("FileBinaryReplaceMe", fileString)
    let response = extensionsPostFormAPIs (loginInfo, "ParsePackage", myPostString, withLog)
    response

//////////////////////////////////Dnn.PersonaBar.Licensing Starts////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Licensing+-+Request+examples///
/////////////////////////////////////////////////////////////////////////////////////////////
let private licensingGetAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let licensingURI = "Licensing"
    let request =
            getFrom ("http://"+siteURL+"/API/PersonaBar/"+licensingURI+"/"+pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private licensingPostAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let licensingURI = "Licensing"
    let mutable myPostString = postString
    let siteURL = setAPIURL()

    let request =
            postTo ("http://"+siteURL+"/API/PersonaBar/"+licensingURI+"/"+pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString
    let response = request |> getResponse
    response

//////////////////////////////////Dnn.PersonaBar.Servers Starts////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Servers+-+Request+examples///
///////////////////////////////////////////////////////////////////////////////////////////////
let private webServersGetAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://"+siteURL+"/API/PersonaBar/SystemInfoWeb/"+pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private serversGetAPIs (loginInfo:UserLoginInfo, pathName:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://"+siteURL+"/API/PersonaBar/"+pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response

let private serversPostAPIs (loginInfo:UserLoginInfo, pathName:string, postString:string, withLog:bool) =
    let mutable myPostString = postString
    let siteURL = setAPIURL()

    let request =
            postTo ("http://"+siteURL+"/API/PersonaBar/"+pathName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString myPostString
    let response = request |> getResponse
    response

let apiServersGetWebServerHostSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "WebServers/GetWebServerHostSettings", withLog)
    response

let apiServersGetWebServersInfo(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "WebServers/GetWebServersInfo", withLog)
    response

let apiSysGetWebServersInfo(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "GetWebServerInfo?", withLog)
    response

let apiServersGetSMTPSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "ServerSettingsSmtpHost/GetSmtpSettings", withLog)
    response

let apiServersGetPerformanceSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "ServerSettingsPerformance/GetPerformanceSettings", withLog)
    response

let apiServersGetLogSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "ServerSettingsLogs/GetLogs", withLog)
    response

let apiServersGetAppInfo(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "SystemInfoApplicationHost/GetApplicationInfo", withLog)
    response

let apiServersGetDBInfo(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "SystemInfoDatabase/GetDatabaseServerInfo", withLog)
    response

let apiServersGetCachedItemList(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "WebServers/GetCacheItemsList", withLog)
    response

let apiServersGetCachedItem(loginInfo:UserLoginInfo, cachedKey:string, withLog:bool) =
    let myCachedKey =
        match cachedKey with
            | "" -> """DNN_ProfileDefinitions-1"""
            | _ -> cachedKey
    let response = serversGetAPIs (loginInfo, "WebServers/GetCacheItem?cacheKey="+myCachedKey, withLog)
    response

let apiServersClearCache(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversPostAPIs (loginInfo, "Server/ClearCache", "", withLog)
    response

let apiServersRestartApplication(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversPostAPIs (loginInfo, "Server/RestartApplication", "", withLog)
    sleep 5
    response

let apiServersSendTestEmail(loginInfo:UserLoginInfo, postString:string, withLog:bool) =
    let myPostString =
        match postString with
            | "" -> """{"smtpServerMode":"h","smtpServer":"127.0.0.1","smtpAuthentication":"0","smtpUsername":"","smtpPassword":"","enableSmtpSsl":false}"""
            | _ -> postString
    let response = serversPostAPIs (loginInfo, "ServerSettingsSmtpHost/SendTestEmail", myPostString, withLog)
    response

let apiServersUpdateSMTPSettings(loginInfo:UserLoginInfo, postString:string, withLog:bool) =
    let myPostString =
        match postString with
            | "" -> """{"smtpServerMode":"h","smtpServer":"127.0.0.1","smtpConnectionLimit":1,"smtpMaxIdleTime":0,"smtpAuthentication":"","smtpUsername":"","smtpPassword":"","enableSmtpSsl":false,"messageSchedulerBatchSize":50}"""
            | _ -> postString
    let response = serversPostAPIs (loginInfo, "ServerSettingsSmtpHost/UpdateSmtpSettings", myPostString, withLog)
    response

let apiServersIncreaseHostVersion(loginInfo:UserLoginInfo, withLog:bool) =
    let response = serversPostAPIs (loginInfo, "ServerSettingsPerformance/IncrementHostVersion", "", withLog)
    response

let apiServersUpdatePerformanceSettings(loginInfo:UserLoginInfo, postString:string, withLog:bool) =
    let myPostString =
        match postString with
            | "" -> """{"CachingProvider":"WebRequestCachingProvider","PageStatePersistence":"P","ModuleCacheProvider":"MemoryModuleCachingProvider","PageCacheProvider":"","CacheSetting":3,"AuthCacheability":"4","UnauthCacheability":"4","SslForCacheSynchronization":false,"ClientResourcesManagementMode":"h","CurrentHostVersion":152,"HostEnableCompositeFiles":false,"HostMinifyCss":false,"HostMinifyJs":false}"""
            | _ -> postString
    let response = serversPostAPIs (loginInfo, "ServerSettingsPerformance/UpdatePerformanceSettings", myPostString, withLog)
    response

let apiServersGetLogFile(loginInfo:UserLoginInfo, fileName:string, withLog:bool) =
    let response = serversGetAPIs (loginInfo, "ServerSettingsLogs/GetLogFile?fileName="+fileName, withLog)
    response

//////////////////////////////////DesktopModules/DNNCorp/ResourceLibrary/API Starts////////////////////////////////
//https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Tabs+-+Request+examples///
/////////////////////////////////////////////////////////////////////////////////////////////
let private rlGetAPIs (loginInfo:UserLoginInfo, apiName:string, queryString:string, withLog:bool) =
    let mutable myQueryString = apiName + queryString
    let response = forAllGetAPIs (loginInfo, "/DesktopModules/DNNCorp/ResourceLibrary/API/Card/", myQueryString, withLog)
    response

let private rlPostAPIs (loginInfo:UserLoginInfo, apiName:string, postString:string, tabID:string, moduleId:string, withLog:bool) =
    let myDefaultTabID = match tabID with
                            | "" | "0" -> ""
                            | _ -> tabID
    let mutable myTabID : NameValuePair = {Name = "TabId"; Value = myDefaultTabID }
    let myGroupId : NameValuePair = {Name = "groupid"; Value = "-1" }
    let myModuleId : NameValuePair = {Name = "moduleid"; Value = moduleId }
    let siteURL = setAPIURL()
    let request =
            postTo ("http://" + siteURL + "/DesktopModules/DNNCorp/ResourceLibrary/API/Card/" + apiName)
            |> Request.setHeader jsonContentTypeHeader
            |> Request.setHeader (customHeader loginInfo.RVToken)
            |> Request.setHeader (customHeader myTabID)
            |> Request.setHeader (customHeader myGroupId)
            |> Request.setHeader (customHeader myModuleId)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
            |> Request.cookie (customCookie loginInfo.RVCookie)
            |> Request.bodyString postString

    let response = request |> getResponse
    response

let apiRLAddCard(loginInfo:UserLoginInfo, pageId:string, moduleInstanceId:string, withLog:bool) =
    let mutable myPostString = ""
    //for now we take fileId=88, fileName=cavalier-tv.png, ImageUrl="/Portals/0/Images/cavalier-tv.png"
    myPostString <- """{"title":"titleReplaceMe","description":"descriptionReplaceMe","imageUrl":"/Portals/0/Images/cavalier-tv.png","fileId":88,"imageFileId":88,"clickBehavior":1,"isFeatured":true,"tags":["automationBVT"],"redirectionUrl": "dnntest.com"}"""
    myPostString <- myPostString.Replace ("titleReplaceMe", "RL-"+Guid.NewGuid().ToString())
    let response = rlPostAPIs (loginInfo, "AddCard", myPostString, pageId, moduleInstanceId, withLog)
    response

let apiRLGetCard(loginInfo:UserLoginInfo, fileName:string, queryString:string, withLog:bool) =
    let response = rlGetAPIs (loginInfo, "GetCards", queryString, withLog)
    response

//////////////////////////////////DesktopModules/DNNCorp/ResourceLibrary/API Starts////////////////////////////////
//   API/SiteExportImport/ExportImport   ///
/////////////////////////////////////////////////////////////////////////////////////////////
let private siteEIGetAPIs (loginInfo:UserLoginInfo, apiName:string, queryString:string, withLog:bool) =
    let mutable myQueryString = apiName + queryString
    let response = forAllGetAPIs (loginInfo, "/API/SiteExportImport/ExportImport/", myQueryString, withLog)
    response

let private siteEIPostAPIs (loginInfo:UserLoginInfo, apiName:string, postString:string, withLog:bool) =
    //let mutable myQueryString = apiName
    let response = forAllPostAPIs (loginInfo, "/API/SiteExportImport/ExportImport/", apiName, postString, "", withLog)
    response

let getSiteExportImportPagesString (tabsResponse:Response, setCheckState:int) =
    let samples = JsonValue.Parse(tabsResponse |> getBody)
    let rtnArray = samples.GetProperty("Results").GetProperty("ChildTabs").AsArray()
    // Parse & re-organize the data for Export purpose. This is for "Pages" section
    // https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Sites+-+Request+examples
    let myCheckState =
        match setCheckState with
            | 0 -> "0"
            | 1 -> "1"
            | -100 -> "random"
            | _ -> setCheckState.ToString()

    let data = rtnArray |> Array.map (fun oneTab -> """{"TabId":""" + oneTab?TabId.AsString() + ""","ParentTabId":""" + oneTab?ParentTabId.AsString() + ""","CheckedState":""" + myCheckState + """}""")
    let pagesStr = "[" + (data |> Seq.map string |> String.concat ",") + """, {"TabId": "-1", "ParentTabId": -1, "CheckedState": 0}]"""
    pagesStr

let apiSiteEIGetAllJobs(loginInfo:UserLoginInfo, portalId:string, jobType:string, withLog:bool) =
    let myPortalId =
        match portalId with
            | "" -> "0"
            | _ -> portalId
    let myJobType =
        match jobType with
            | "" -> "null"
            | _ -> jobType
    let myQueryString = "?portal="+myPortalId+"&pageIndex=0&pageSize=10&jobType="+myJobType+"&keywords="
    let response = siteEIGetAPIs (loginInfo, "AllJobs", myQueryString, withLog)
    response

let apiSiteEIPostExport(loginInfo:UserLoginInfo, portalId:string, pagesSelected:string, withLog:bool) =
    let myPortalId =
        match portalId with
            | "" -> "0"
            | _ -> portalId

    let mutable myPostString = """{"PortalId":portalIdReplaceMe,"ExportName":"ExportNameReplaceMe","ExportDescription":"ExportDescriptionReplaceMe","IncludeUsers":true,"IncludeVocabularies":true,"IncludeTemplates":true,"IncludeProperfileProperties":true,"IncludeRoles":true,"IncludePermissions":true,"IncludeDeletions":false,"IncludeContent":true,"IncludeFiles":true,"ExportMode":"Full","ItemsToExport":[],"pages":pagesReplaceMe}"""
    myPostString <- myPostString.Replace("portalIdReplaceMe", portalId)
    myPostString <- myPostString.Replace("ExportNameReplaceMe", "Exp-"+Guid.NewGuid().ToString())
    myPostString <- myPostString.Replace("ExportDescriptionReplaceMe", "Automated API Testing for Site Export/Import")
    myPostString <- myPostString.Replace("pagesReplaceMe", pagesSelected)
    let response = siteEIPostAPIs (loginInfo, "Export", myPostString, withLog)
    response

//////////////////////////////////DesktopModules/DNNCorp/ResourceLibrary/API Starts////////////////////////////////
//   API/PersonaBar/Components/   ///
/////////////////////////////////////////////////////////////////////////////////////////////
let private componentsGetAPIs (loginInfo:UserLoginInfo, apiName:string, queryString:string, withLog:bool) =
    let mutable myQueryString = apiName + queryString
    let response = forAllGetAPIs (loginInfo, "/API/PersonaBar/Components/", myQueryString, withLog)
    response

let private componentsPostAPIs (loginInfo:UserLoginInfo, apiName:string, postString:string, withLog:bool) =
    //let mutable myQueryString = apiName
    let response = forAllPostAPIs (loginInfo, "/API/PersonaBar/Components/", apiName, postString, "", withLog)
    response

let apiComponentsGetSuggestionRoles(loginInfo:UserLoginInfo, roleGroupId:string, keyword:string, withLog:bool) =
    let myKeyword = keyword
    let myRoleGroupId =
        match roleGroupId with
            | "" -> "-1"
            | _ -> roleGroupId

    //?roleGroupId=-1&count=10&keyword=trans
    let myQueryString = "?roleGroupId="+myRoleGroupId+"&count=100&keyword="+myKeyword
    let response = componentsGetAPIs (loginInfo, "GetSuggestionRoles", myQueryString, withLog)
    response

let apiComponentsGetSuggestionUsers(loginInfo:UserLoginInfo, keyword:string, withLog:bool) =
    let myKeyword = keyword
    //?count=10&keyword=trans
    let myQueryString = "?count=100&keyword="+myKeyword
    let response = componentsGetAPIs (loginInfo, "GetSuggestionUsers", myQueryString, withLog)
    response

//////////////////////////////////DesktopModules/DNNCorp/Security/API Starts////////////////////////////////
//   API/PersonaBar/Security/   ///
/////////////////////////////////////////////////////////////////////////////////////////////
let private securityGetAPIs (loginInfo:UserLoginInfo, apiName:string, queryString:string, withLog:bool) =
    let mutable myQueryString = apiName + queryString
    let response = forAllGetAPIs (loginInfo, "/API/PersonaBar/Security/", myQueryString, withLog)
    response
let private securityPostAPIs (loginInfo:UserLoginInfo, apiName:string, postString:string, withLog:bool) =
    //let mutable myQueryString = apiName
    let response = forAllPostAPIs (loginInfo, "/API/PersonaBar/Components/", apiName, postString, "", withLog)
    response
let apiSecurityGetBasicLoginSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let myQueryString = "?cultureCode=en-US"
    let response = securityGetAPIs (loginInfo, "GetBasicLoginSettings", myQueryString, withLog)
    response
let apiSecurityGetIPFilter(loginInfo:UserLoginInfo, withLog:bool) =
    let myQueryString = ""
    let response = securityGetAPIs (loginInfo, "GetIpFilters", myQueryString, withLog)
    response
let apiSecurityGetMemberSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let myQueryString = ""
    let response = securityGetAPIs (loginInfo, "GetMemberSettings", myQueryString, withLog)
    response
let apiSecurityGetRegistrationSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let myQueryString = ""
    let response = securityGetAPIs (loginInfo, "GetRegistrationSettings", myQueryString, withLog)
    response

//apiSecurityUpdateBasicLoginSettings
let apiSecurityUpdateBasicLoginSettings(loginInfo:UserLoginInfo, withLog:bool) =
    let myPostString = """{"DefaultAuthProvider":"DNN","PrimaryAdministratorId":2,"RedirectAfterLoginTabId":-1,"RedirectAfterLoginTabName":"","RedirectAfterLoginTabPath":"","RedirectAfterLogoutTabId":-1,"RedirectAfterLogoutTabName":"","RedirectAfterLogoutTabPath":"","RequireValidProfileAtLogin":true,"CaptchaLogin":false,"CaptchaRetrivePassword":false,"CaptchaChangePassword":false,"HideLoginControl":false,"cultureCode":"en-US"}"""
    let response = securityPostAPIs (loginInfo, "UpdateBasicLoginSettings", myPostString, withLog)
    response

// Need to use SQL module to execute the query to set up PB permission
let setPBMenuPermission(loginInfo:UserLoginInfo, portalId:string, roleId:string, menuIdentifier:string, permissionKey:string, allowAccess:string, withLog:bool) =
    let mutable mySQL = MySqlScript
    mySQL <- mySQL.Replace("IdentifierReplaceMe", menuIdentifier)
    mySQL <- mySQL.Replace("PortalIdReplaceMe", portalId)
    mySQL <- mySQL.Replace("RoleIdReplaceMe", roleId)
    mySQL <- mySQL.Replace("PermissionKeyReplaceMe", permissionKey)
    mySQL <- mySQL.Replace("AllowAccessReplaceMe", allowAccess)

    let response = postRunSQLQuery (loginInfo, mySQL, true)
    response

let validatePBMenuByIdentifierName (pbIdName:string )=
    //reload page
    DnnCommon.reloadPage()
    let result =  (element "//div[contains(@id,'PersonaBarPanel')]/script").Text
    printfn "a is %A" result
    let a = "0"
    printfn "a is %A" a

let getPBMenuDefaultPermission (loginInfo:UserLoginInfo, identifierName:string, withLog:bool) =

    let mutable mySQL = MySqlScriptViewPageListDefaultPermission
    mySQL <- mySQL.Replace("IdentifierReplaceMe", identifierName)
    let response = postRunSQLQuery (loginInfo, mySQL, true)
    response

// Need to use SQL module to execute the query to set up PB permission
// allowAccessView & allowAccessEdit : "1"=allow; "0"=deny
let setPBMenuPermissionsAll(loginInfo:UserLoginInfo, portalId:string, roleId:string, allowAccessView:string, allowAccessEdit:string, withLog:bool) =
    for pbItem in PBIdentifierNames do
        // Turn on the View Permission
        use response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, pbItem, "view", allowAccessView, true)
        PBPermissionRead <- allowAccessView
        printf "\nSet %A View Permission = %A"  pbItem allowAccessView
        use response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, pbItem, "edit", allowAccessEdit, true)
        PBPermissionEdit <- allowAccessEdit
        printf "\nSet %A Edit Permission = %A"  pbItem allowAccessEdit

let setPBMenuPermissions(loginInfo:UserLoginInfo, portalId:string, roleId:string, pbIdName:string, allowAccessView:string, allowAccessEdit:string, withLog:bool) =
    // Turn on the View Permission
    use response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, pbIdName, "view", allowAccessView, true)
    PBPermissionRead <- allowAccessView
    printf "\nSet %A View Permission = %A"  pbIdName allowAccessView
    use response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, pbIdName, "edit", allowAccessEdit, true)
    PBPermissionEdit <- allowAccessEdit
    printf "\nSet %A Edit Permission = %A"  pbIdName allowAccessEdit

// Adding Extra Permission Keywords for Users
let setPBMenuPermissionsUsers(loginInfo:UserLoginInfo, portalId:string, roleId:string,  allowAccessView:string, allowAccessEdit:string,withLog:bool) =
    for pbItemIdentifier in PBIdentifierNamesForUsersView do
        let response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, "Dnn.Users", pbItemIdentifier, allowAccessView, true) //SITE_INFO_EDIT
        PBPermissionUsersView <- allowAccessView
        printf "\nSet %A View Permission = %A"  pbItemIdentifier allowAccessView
    for pbItemIdentifier in PBIdentifierNamesForUsersEdit do
        let response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, "Dnn.Users", pbItemIdentifier, allowAccessEdit, true) //SITE_INFO_EDIT
        PBPermissionUsersEdit <- allowAccessEdit
        printf "\nSet %A Edit Permission = %A"  pbItemIdentifier allowAccessEdit

// Adding Extra Permission Keywords for AdminLogs
let setPBMenuPermissionsAdminLogs(loginInfo:UserLoginInfo, portalId:string, roleId:string,  allowAccessView:string, allowAccessEdit:string, withLog:bool) =
    for pbItemIdentifier in PBIdentifierNamesForAdminLogView do
        let response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, "Dnn.AdminLogs", pbItemIdentifier, allowAccessView, true)
        PBPermissionAdminLogView <- allowAccessView
        printf "\nSet %A View Permission = %A"  pbItemIdentifier allowAccessView
    for pbItemIdentifier in PBIdentifierNamesForAdminLogEdit do
        let response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, "Dnn.AdminLogs", pbItemIdentifier, allowAccessEdit, true)
        PBPermissionAdminLogEdit <- allowAccessEdit
        printf "\nSet %A Edit Permission = %A"  pbItemIdentifier allowAccessEdit

// Adding Extra Permission Keywords for Recycle Bin
let setPBMenuPermissionsRecycleBin(loginInfo:UserLoginInfo, portalId:string, roleId:string,  allowAccessView:string, allowAccessEdit:string, withLog:bool) =
    for pbItemIdentifier in PBIdentifierNamesForRecycleBinView do
        let response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, "Dnn.Recyclebin", pbItemIdentifier, allowAccessView, true)
        PBPermissionRecycleBinView <- allowAccessView
        printf "\nSet %A View Permission = %A"  pbItemIdentifier allowAccessView
    for pbItemIdentifier in PBIdentifierNamesForRecycleBinEdit do
        let response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, "Dnn.Recyclebin", pbItemIdentifier, allowAccessEdit, true)
        PBPermissionRecycleBinEdit <- allowAccessEdit
        printf "\nSet %A Edit Permission = %A"  pbItemIdentifier allowAccessEdit

let setPBMenuPermissionsSecurity(loginInfo:UserLoginInfo, portalId:string, roleId:string,  allowAccessView:string, allowAccessEdit:string, withLog:bool) =
    for pbItemIdentifier in PBIdentifierNamesForSecurityView do
        let response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, "Dnn.Security", pbItemIdentifier, allowAccessView, true)
        PBPermissionSecurityView <- allowAccessView
        printf "\nSet %A View Permission = %A"  pbItemIdentifier allowAccessView
    for pbItemIdentifier in PBIdentifierNamesForSecurityEdit do
        let response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, "Dnn.Security", pbItemIdentifier, allowAccessEdit, true)
        PBPermissionSecurityEdit <- allowAccessEdit
        printf "\nSet %A Edit Permission = %A"  pbItemIdentifier allowAccessEdit

let setPBMenuPermissionsSiteInfo(loginInfo:UserLoginInfo, portalId:string, roleId:string, allowAccessSiteInfoView:string, allowAccessSiteInfoEdit:string, withLog:bool) =
    for pbItemIdentifier in PBIdentifierNamesForSiteInfo do
        // Turn on/off the SiteInfo Permission
        //   setPBMenuPermission(loginInfo:UserLoginInfo, portalId:string, roleId:string, menuIdentifier:string, permissionKey:string, allowAccess:string, withLog:bool) =
        use response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, pbItemIdentifier, "SITE_INFO_VIEW", allowAccessSiteInfoView, true) //SITE_INFO_EDIT
        PBPermissionSiteInfoView <- allowAccessSiteInfoView
        printf "\nSet %A View Permission = %A"  pbItemIdentifier allowAccessSiteInfoView
        use response = setPBMenuPermission(defaultHostLoginInfo, portalId, roleId, pbItemIdentifier, "SITE_INFO_EDIT", allowAccessSiteInfoEdit, true) //SITE_INFO_EDIT
        PBPermissionSiteInfoEdit <- allowAccessSiteInfoEdit
        printf "\nSet %A View Permission = %A"  pbItemIdentifier allowAccessSiteInfoEdit

// so far only for Registered Users
// "read", "edit", "readedit", "", the permission
let assertTestForPBPermission (response:Response, readOrEdit:string) =
    match readOrEdit with
        | "post" ->
            if PBPermissionEdit = "1" then
                Check.AreEqual(response.statusCode, 200)
            else
                Check.AreEqual(response.statusCode, 401)
            |> ignore
        | "get" ->
            if PBPermissionRead = "1" then
                Check.AreEqual(response.statusCode, 200)
            else
                Check.AreEqual(response.statusCode, 401)
            |> ignore
        | _ -> printf "assertTestForPBPermission> Response.statusCode = %A" response.statusCode

let assertTestForPBPermissionSiteInfo (response:Response, readOrEdit:string) =
    match readOrEdit with
        | "post" ->
            if PBPermissionSiteInfoEdit = "1" then Check.AreEqual(response.statusCode, 200) else Check.AreEqual(response.statusCode, 401)
            |> ignore
        | "get" ->
            if PBPermissionSiteInfoView = "1" then Check.AreEqual(response.statusCode, 200) else Check.AreEqual(response.statusCode, 401)
            |> ignore
        | _ -> printf "assertTestForPBPermission> Response.statusCode = %A" response.statusCode

let getAPISecurityAnalyzer (loginInfo:UserLoginInfo, domainURL:string, actionURL:string, withLog:bool) =
    let siteURL = setAPIURL()
    let request =
            getFrom ("http://" + siteURL + domainURL + actionURL)
            |> Request.cookie (customCookie loginInfo.DNNCookie)
    let response = request |> getResponse
    //printfn "response is %A" response
    response
