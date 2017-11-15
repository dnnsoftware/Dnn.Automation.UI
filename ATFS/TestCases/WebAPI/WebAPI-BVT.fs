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

module WebAPIBVT

open HttpFs.Client
open FSharp.Data
open FSharp.Data.JsonExtensions
open canopy
open DnnCanopyContext
open DnnTypes
open DnnWebApi
open DnnUserLogin
open APIData
open APIHelpers

//page info
let private loginPage = "/Login"

type private RoleTypeToTest =
    | HOSTUSER
    | ADMINISTRATORS

type private UserRole = 
    { LogInUserRole : string }

let private bvtLogInAccountsPreSet roleName = 
    context "WebAPI BVT"
    (sprintf "WebAPI Prepare for Users in role %A" roleName) @@@ fun _ -> 
        let hostUser = apiLoginAsHost()
        // we assume WEB API tests are run separately so we disable image capturing
        // note if anything else runs after the API tests, it will not capture error screenshots
        canopy.configuration.failureScreenshotsEnabled <- false
        let userNamePrefix = 
            if useChildPortal then "C"
            else ""

        let userName = 
            match roleName with
            | APIRoleName.HOSTUSER -> "host"
            | APIRoleName.ADMINISTRATORS -> userNamePrefix + "AutoADMIN"
            | APIRoleName.ANONYMOUS -> "ANONYMOUS"
            | _ -> userNamePrefix + "AutoTesterRU"

        if roleName.ToString() <> "HOSTUSER" && roleName.ToString() <> "ANONYMOUS" then 
            let rtnUserId = apiUsersIfUserExists (hostUser, userName)
            if rtnUserId < 0 then // User does not exist
                let newUserInfo : APICreateUserInfo = 
                    { FirstName = roleName.ToString()
                      LastName = "DnnTester"
                      UserName = userName
                      Password = config.Site.DefaultPassword
                      EmailAddress = roleName.ToString() + "DnnTester@mailinator.com"
                      DisplayName = roleName.ToString() + roleName.ToString()
                      UserID = "0"
                      Authorize = "true" }

                use createdUser = apiCreateUser (hostUser, newUserInfo, true)
                let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
                let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
                apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName
                Check.GreaterOrEqual (createdUserId.AsInteger()) 1
            else Check.GreaterOrEqual rtnUserId 1
        if useChildPortal && SiteID <= 0 then 
            let myRtnSiteID = createChildPortalwithWaiting config.Site.ChildSitePrefix
            if myRtnSiteID > 0 then SiteID <- myRtnSiteID
            else 
                let responseSite = apiSitesGetPortalAny (hostUser, config.Site.ChildSitePrefix, true)
                SiteID <- JsonValue.Parse(responseSite |> getBody).GetProperty("Results").[0].GetProperty("PortalID").AsInteger()

let private bvtLogInDataPreSet roleName = 
    context "WebAPI BVT"
    (sprintf "WebAPI Prepare for Users in role - %A" roleName) @@@ fun _ -> 
        match roleName with
        | APIRoleName.ADMINISTRATORS -> myLoginInfo <- apiLoginAsAdmin()
        | APIRoleName.HOSTUSER -> 
            loginAsHost()
            let allCookieString = browser.Manage().Cookies.AllCookies
            let userCookie = browser.Manage().Cookies.GetCookieNamed(".DOTNETNUKE")
            let rvCookie = browser.Manage().Cookies.GetCookieNamed("__RequestVerificationToken")
            let myRVToken = getRequestVerificationToken (false, true)
            myLoginInfo <- {
              UserName = config.Site.HostUserName
              Password = config.Site.DefaultPassword
              DisplayName = config.Site.HostDisplayName
              DNNCookie = { Name = userCookie.Name; Value = userCookie.Value }
              RVCookie = { Name = rvCookie.Name; Value = rvCookie.Value }
              RVToken = { Name = "RequestVerificationToken"; Value = myRVToken }
              }
        | _ -> myLoginInfo <- apiLoginAsRU()

let private promptCommandLine testRole = 
    context "WebAPI BVT"
    "WebAPI CmdLine - GetList -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = ""
        let response = apiGetCmdList (myLoginUserInfo, portalID, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = response |> getBody
            let sample = JsonValue.Parse(body).AsArray()
            let records = sample.GetUpperBound(0)
            Check.GreaterOrEqual records 42
    "WebAPI CmdLine - Clear-log -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"clear-log","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let isError = sample.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = sample.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 0
    "WebAPI CmdLine - Goto -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"goto 20","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let isError = sample.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = sample.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 0
    "WebAPI CmdLine - Get-Page -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"get-page 20","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let isError = sample.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = sample.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 1
    "WebAPI CmdLine - List-Modules -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-modules","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let isError = sample.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = sample.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 10
    "WebAPI CmdLine - Get-Module -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-modules","currentPage":"20"}"""
        let response = apiCmdLine (defaultHostLoginInfo, portalID, commandLine, true)
        //Validation by user role
        let body = response |> getBody
        let sample = JsonValue.Parse(body)
        let moduleIDs = sample.GetProperty("data").AsArray()
        let moduleId = moduleIDs.[0].GetProperty("ModuleId").AsString()
        let pageId = moduleIDs.[0].GetProperty("AddedToPages").AsString()
        let commandLine = "{\"cmdLine\":\"get-module " + moduleId + " --pageid " + pageId + "\",\"currentPage\":\"20\"}"
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let isError = sample.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = sample.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 1
    "WebAPI CmdLine - Get-Module -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-modules","currentPage":"20"}"""
        let response = apiCmdLine (defaultHostLoginInfo, portalID, commandLine, true)
        //Validation by user role
        let body = response |> getBody
        let sample = JsonValue.Parse(body)
        let moduleIDs = sample.GetProperty("data").AsArray()
        let moduleId = moduleIDs.[0].GetProperty("ModuleId").AsString()
        let pageId = moduleIDs.[0].GetProperty("AddedToPages").AsString()
        let commandLine = "{\"cmdLine\":\"get-module " + moduleId + " --pageid " + pageId + "\",\"currentPage\":\"20\"}"
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let isError = sample.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = sample.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 1
    "WebAPI CmdLine - Copy-Module -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-modules","currentPage":"20"}"""
        let response = apiCmdLine (defaultHostLoginInfo, portalID, commandLine, true)
        //Validation by user role
        let body = response |> getBody
        let sample = JsonValue.Parse(body)
        let moduleIDs = sample.GetProperty("data").AsArray()
        let moduleId = moduleIDs.[0].GetProperty("ModuleId").AsString()
        let pageId = moduleIDs.[0].GetProperty("AddedToPages").AsString()
        let commandLine = "{\"cmdLine\":\"copy-module " + moduleId + " --pageid " + pageId + " --topageid " + pageId + "\",\"currentPage\":\"20\"}"
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 1
    "WebAPI CmdLine - Delete-Module -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-modules","currentPage":"20"}"""
        let response = apiCmdLine (defaultHostLoginInfo, portalID, commandLine, true)
        //Validation by user role
        let body = JsonValue.Parse(response |> getBody)
        let moduleIDs = body.GetProperty("data").AsArray()
        let moduleId = moduleIDs.[0].GetProperty("ModuleId").AsString()
        let pageId = moduleIDs.[0].GetProperty("AddedToPages").AsString()
        let commandLine = "{\"cmdLine\":\"delete-module " + moduleId + " --pageid " + pageId + "\",\"currentPage\":\"20\"}"
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual 1 records
    "WebAPI CmdLine - List-Pages -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-pages","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 1
    "WebAPI CmdLine - New-Page -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let pageName = "Prompt" + System.Guid.NewGuid().ToString()
        let commandLine = "{\"cmdLine\":\"new-page \\\"" + pageName + "\\\"\",\"currentPage\":\"20\"}"
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 1
    "WebAPI CmdLine - List-Users -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-users","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 1
    "WebAPI CmdLine - list-roles -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-roles","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 3
    "WebAPI CmdLine - list-tasks -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"list-tasks","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 6
    "WebAPI CmdLine - get-host -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"get-host","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 0
    "WebAPI CmdLine - Clear-Cache -" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let commandLine = """{"cmdLine":"clear-cache","currentPage":"20"}"""
        let response = apiCmdLine (myLoginUserInfo, portalID, commandLine, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
        | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
        | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then 
            let body = JsonValue.Parse(response |> getBody)
            let isError = body.GetProperty("isError").AsBoolean()
            Check.AreEqual false isError
            let records = body.GetProperty("records").AsInteger()
            Check.GreaterOrEqual records 0

//tests
let private aqlConsoleRunQuery testRole = 
    context "WebAPI BVT"
    "WebAPI Run SQL Query-" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = postRunSQLQuery (myLoginUserInfo, """select * from users""", true)
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then assertTestForPBPermission (response, "post")
        else 
            match testRole with
            | APIRoleName.HOSTUSER -> Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

let private aqlConsoleSaveQuery testRole =
    context "WebAPI BVT"
    "WebAPI SAVE SQL Query-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        //for c:OpenQA.Selenium.Cookie in allCookies do
        let response = PostSaveSQLQuery (myLoginUserInfo, """select * from users""", "", true)
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER  ->
                    Check.AreEqual 200 response.statusCode
                | _ ->
                    Check.AreEqual 401 response.statusCode

let private aqlConsoleGetSavedQuery testRole =
    context "WebAPI BVT"
    "WebAPI Get Saved SQL Query-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        // Step1 : Save 1+ Query
        let myQueryName = "Q" + System.Guid.NewGuid().ToString()
        let response1 = PostSaveSQLQuery (loginInfo, """select * from users""", myQueryName, false)
        let response2 = GetSavedSQLQuery (myLoginUserInfo, true)
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response2, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER  ->
                    Check.AreEqual 200 response2.statusCode
                | _ ->
                    Check.AreEqual 401 response2.statusCode

let private aqlConsoleGetSavedQueryById testRole =
    context "WebAPI BVT"
    "WebAPI Get Saved SQL Query By Id-"+testRole.ToString() @@@ fun _ ->
        let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        // Step1 : Save 1+ Query
        let myQueryName = "Q" + System.Guid.NewGuid().ToString()
        let response1 = PostSaveSQLQuery (loginInfo, """select * from users""", myQueryName, false)
        let response2 = GetSavedSQLQuery (loginInfo, false)
        let sample2 = JsonValue.Parse(response2 |> getBody)
        let myQId = sample2.GetProperty("queries").[0].GetProperty("id").AsString()
        let myQName = sample2.GetProperty("queries").[0].GetProperty("name").AsString()

        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response3 = GetSavedSQLQueryById (myLoginUserInfo, myQId, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response3, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER  ->
                    Check.AreEqual 200 response3.statusCode
                    let sample3 = JsonValue.Parse(response3 |> getBody)
                    let myRtnQName = sample3.GetProperty("name").AsString()
                    Check.AreEqual myQName myRtnQName
                | _ ->
                    Check.AreEqual 401 response3.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private aqlConsoleDeleteQueryById testRole =
    context "WebAPI BVT"
    "WebAPI Delete SQL Query By Id-"+testRole.ToString() @@@ fun _ ->
        let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        // Step1 : Save 1+ Query
        let myQueryName = "Q" + System.Guid.NewGuid().ToString()
        let response1 = PostSaveSQLQuery (loginInfo, """select * from users""", myQueryName, false)
        let sample1 = JsonValue.Parse(response1 |> getBody)
        let myQId = sample1.GetProperty("id").AsString()
        let myQName = sample1.GetProperty("name").AsString()
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response2 = DeleteSQLQueryById (myLoginUserInfo, myQId, true)
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response2, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER  ->
                    Check.AreEqual 200 response2.statusCode
                | _ ->
                    Check.AreEqual 401 response2.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private CustomCSSGet testRole =
    context "WebAPI BVT"
    "WebAPI Host Get Custom CSS-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getHostCSSEditor (myLoginUserInfo, true)
        //printfn "response is %A" response
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER  | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private CustomCSSGetNegative _ =
    context "WebAPI BVT"

    "WebAPI Host Get Custom CSS - Empty Cookie" @@@ fun _ ->
        // Provide empty cookie and/or null cookie
        logOff()
        let myRVToken = getRequestVerificationToken(true, true)
        let allCookieString = browser.Manage().Cookies.AllCookies
        let userCookie = browser.Manage().Cookies.GetCookieNamed(".DOTNETNUKE")
        let mutable myCookie: NameValuePair =  { Name=userCookie.Name; Value="" }
        let mutable response = getHostCSSEditor (loginInfo, true)
        Check.AreEqual 401 response.statusCode
        let samples = JsonValue.Parse(response |> getBody)
        Check.AreEqual "Authorization has been denied for this request." (samples.GetProperty("Message").AsString())

    "WebAPI Host Get Custom CSS - Null Cookie Value" @@@ fun _ ->
        // Null Cookie Value
        let userCookie = browser.Manage().Cookies.GetCookieNamed(".DOTNETNUKE")
        let myCookie = { Name=userCookie.Name; Value=null}
        let response = getHostCSSEditor (loginInfo, true)
        Check.AreEqual 401 response.statusCode

    "WebAPI Host Get Custom CSS - Empty Cookie" @@@ fun _ ->
        // Empty Cookie
        let myCookie = { Name=""; Value=""}
        let response = getHostCSSEditor (loginInfo, true)
        Check.AreEqual 401 response.statusCode

    "WebAPI Host Get Custom CSS - Wrong Cookie" @@@ fun _ ->
        // Empty Cookie
        let myCookie = { Name=".WRONGCOOKIE"; Value="%true%"}
        let response = getHostCSSEditor (loginInfo, true)
        Check.AreEqual 401 response.statusCode

let private CustomCSSUpdate testRole =
    context "WebAPI BVT"
    "WebAPI Host Save Update Custom CSS-"+testRole.ToString() @@@ fun _ ->
        //defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        //let responseSite = apiSitesGetPortalAny(defaultHostLoginInfo, config.Site.ChildSitePrefix, true)
        //let mySiteID = JsonValue.Parse(responseSite |> getBody).GetProperty("Results").[0].GetProperty("PortalID").AsInteger()
        //printfn "SiteID is %A" SiteID
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = postHostCSSEditor (myLoginUserInfo, SiteID, true)
        //printfn "response is %A" response
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private CustomCSSRestore testRole =
    context "WebAPI BVT"
    "WebAPI Host Restore Custom CSS-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = restoreHostCSSEditor (myLoginUserInfo, SiteID, true)
        //printfn "response is %A" response
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private ConfigFileListGet testRole =
    context "WebAPI BVT"
    "WebAPI Host Get Config File List-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        //ConfigConsole/GetConfigFilesList
        let response = getConfigFileList (myLoginUserInfo, true)
        //printfn "response is %A" response
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER  ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let totalResults = samples.GetProperty("TotalResults").AsInteger()
                    Check.GreaterOrEqual totalResults 6
                | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private ConfigFileGetByName testRole =
    context "WebAPI BVT"
    "WebAPI Host Get Config File by FileName-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getConfigFileByName (myLoginUserInfo, "DotNetNuke.config", true)
        //printfn "response is %A" response
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER  ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let rtnFileName = samples.GetProperty("FileName").AsString()
                    Check.AreEqual "DotNetNuke.config" rtnFileName
                | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private validateSuccess response =
    Check.AreEqual 200 response.statusCode
    let samples = JsonValue.Parse(response |> getBody)
    let rtnValue = samples.GetProperty("Success").AsBoolean()
    Check.AreEqual true rtnValue
    ()

let private ConfigFileUpdate testRole =
    context "WebAPI BVT"
    "WebAPI Host Update Config File by FileName-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = updateConfigFileByName (myLoginUserInfo, "DotNetNuke.config", true)
        //printfn "response is %A" response
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
            | APIRoleName.HOSTUSER  ->
                validateSuccess response
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private ConfigFileMerge testRole =
    context "WebAPI BVT"
    "WebAPI Host Merge Config File by FileName-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = mergeConfigFileByName (myLoginUserInfo, "web.config", true)
        //printfn "response is %A" response
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
            | APIRoleName.HOSTUSER  ->
                validateSuccess response
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SiteMapGetProviders testRole =
    context "WebAPI BVT"
    "WebAPI Get All Site Map Providers-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getSiteMapAPIs (myLoginUserInfo, "GetSitemapProviders", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("Providers").AsArray()
            Check.GreaterOrEqual (rtnValue2.GetUpperBound(0)) 0

        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SiteMapUpdateProviders testRole =
    context "WebAPI BVT"
    "WebAPI Update Site Map Providers-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getSiteMapAPIs (myLoginUserInfo, "GetSitemapProviders", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("Providers").AsArray()
            Check.GreaterOrEqual (rtnValue2.GetUpperBound(0)) 0
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SiteMapSettingsGet testRole =
    context "WebAPI BVT"
    "WebAPI Get All Site Map Settings-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getSiteMapAPIs (myLoginUserInfo, "GetSettings", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("SitemapCacheDays").AsInteger()
            Check.GreaterOrEqual rtnValue 1
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SiteMapSearchEngineListGet testRole =
    context "WebAPI BVT"
    "WebAPI Get Search Engine List-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getSiteMapAPIs (myLoginUserInfo, "GetSearchEngineList", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 3
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SiteMapSearchEngineURLGet testRole =
    context "WebAPI BVT"
    "WebAPI Get Search Engine List-"+testRole.ToString() @@@ fun _ ->
        let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let mutable response = getSiteMapAPIs (loginInfo, "GetSearchEngineList", false)
        //printfn "response is %A" response
        Check.AreEqual 200 response.statusCode
        let mutable samples = JsonValue.Parse(response |> getBody)
        let rtnValue = samples.GetProperty("Results").AsArray()
        let mutable rtnValue2 = 1
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        for i in rtnValue do
            let newPath = "GetSearchEngineSubmissionUrl?searchEngine=" + i.AsString()
            response <- getSiteMapAPIs (myLoginUserInfo, newPath, true)
            match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let mutable samples2 = response |> getBody
                rtnValue2 <- samples2.ToString().Length
                Check.GreaterOrEqual rtnValue2 20
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

// expired
let private SiteMapProviderUpdate testRole =
    context "WebAPI BVT"
    "WebAPI Update Site Map Provider-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = updateSiteMapAPIs (myLoginUserInfo, "UpdateProvider", SamplePostUpdateSiteMapProvider, true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            validateSuccess response
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SiteMapSettingsUpate testRole =
    context "WebAPI BVT"
    "WebAPI Update Site Map Settings-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = updateSiteMapAPIs (myLoginUserInfo, "UpdateSettings", SamplePostUpdateSiteMapSettings, true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            validateSuccess response
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SiteMapResetCache testRole =
    context "WebAPI BVT"
    "WebAPI Reset Site Map Cache-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = updateSiteMapAPIs (myLoginUserInfo, "ResetCache", "", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            validateSuccess response
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SiteMapCreateVerification testRole =
    context "WebAPI BVT"
    "WebAPI CREATE SEARCH ENGINE VERIFICATION FILE-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let randfileName = "SiteMapVerfiy-" + System.Guid.NewGuid().ToString()
        let response = updateSiteMapAPIs (myLoginUserInfo, "CreateVerification?verification="+randfileName+".html", "", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            validateSuccess response
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsPortalsGet testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Get Portals-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getUIPortalsAPIs (myLoginUserInfo, "GetPortals?addAll=true", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER  ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 2
        | APIRoleName.ADMINISTRATORS   ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.AreEqual 2 rtnValue2
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsItemsGet testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Get Log Items-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getAdminLogsAPIs (myLoginUserInfo, "GetLogItems?logType=*&pageSize=100&pageIndex=0", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 1
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

    // Test Case to validate with PortalID assigned, it shall only return Log Items of that specific Portal.
    // Host users shall be able to see any portals.
    // Admin users shall also test for its visibility permission since admin is portal level.
    "WebAPI AdminLogs Get Log Items with PortalId-"+testRole.ToString() @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let myPortalId = setAPIPortalId() // try to get current Portal ID
        let response = getAdminLogsAPIs (myLoginUserInfo, "GetLogItems?portalId="+myPortalId.ToString()+"&logType=*&pageSize=100&pageIndex=0", true)

        let responsePortal = apiSiteInfoGetPortalSettings (defaultHostLoginInfo, myPortalId.ToString(), "", false)
        let myPortalName = JsonValue.Parse(responsePortal |> getBody).GetProperty("Settings").GetProperty("PortalName").AsString()

        // Validate by matching the Users with same portal.

        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            // "PortalName" shall all be "My Website" if PortalId=0, matchArray shall be nothing in there
            let logItems = JsonValue.Parse(response |> getBody).GetProperty("Results").AsArray()
            let portalNameArray = logItems |> Array.map (fun oneLogItem -> oneLogItem?LogPortalName.AsString())
            let matchArray = portalNameArray |> Array.choose (fun onePortalName -> if (onePortalName <> myPortalName) then Some(onePortalName) else None)
            Check.AreEqual -1 (matchArray.GetUpperBound(0))
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

    // Test Case exec only on ChildPortal with Admin user of child portal
    // Admin of child portal shall NOT see log items on main portal, but API still returns with childportal log items.
    "WebAPI AdminLogs Get main portal Log Items by portal admin-"+testRole.ToString() @@@ fun _ ->
        let myPortalId = setAPIPortalId()
        if myPortalId > 0 && testRole = APIRoleName.ADMINISTRATORS then
            defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
            let myLoginUserInfo = BVTLogInDataPreparation testRole

            let responsePortal = apiSiteInfoGetPortalSettings (defaultHostLoginInfo, myPortalId.ToString(), "", false)
            let myPortalName = JsonValue.Parse(responsePortal |> getBody).GetProperty("Settings").GetProperty("PortalName").AsString()
            // Validate only return "childsite" log items.
            let response = getAdminLogsAPIs (myLoginUserInfo, "GetLogItems?portalId=0&logType=*&pageSize=100&pageIndex=0", true)
            let logItems = JsonValue.Parse(response |> getBody).GetProperty("Results").AsArray()
            let portalNameArray = logItems |> Array.map (fun oneLogItem -> oneLogItem?LogPortalName.AsString())
            let matchArray = portalNameArray |> Array.choose (fun onePortalName -> if (onePortalName <> myPortalName) then Some(onePortalName) else None)
            Check.AreEqual -1 (matchArray.GetUpperBound(0))

let private AdminLogsItemsDelete testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Delete Log Items-"+testRole.ToString() @@@ fun _ ->
        let mutable myPostString =  SamplePostDeleteLogItems
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        if testRole = APIRoleName.HOSTUSER || testRole = APIRoleName.ADMINISTRATORS then
            // Delete Log Items
            let responseLogs = getAdminLogsAPIs (myLoginUserInfo, "GetLogItems?logType=*&pageSize=1000&pageIndex=0", false)
            let samplesLogs = JsonValue.Parse(responseLogs |> getBody)
            let totalLogCnt = samplesLogs.GetProperty("TotalResults").AsInteger()
            if totalLogCnt > 0 then // If any item exists
                let testId =  samplesLogs.GetProperty("Results").[0].GetProperty("LogGUID").AsString()
                let maxCnt = if totalLogCnt > 1000 then 1000 else totalLogCnt
                let arrayLogs = [| for i in 1..maxCnt -> "\""+samplesLogs.GetProperty("Results").[i-1].GetProperty("LogGUID").AsString()+"\"" |]
                let logIDs = arrayLogs |> Array.ofSeq |> String.concat ","
                myPostString <- myPostString.Replace("LogItemGuIdsReplaceMe", logIDs)
            else
                myPostString <- myPostString.Replace("LogItemGuIdsReplaceMe", "")

            //let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = updateAdminLogsAPIs (myLoginUserInfo, "DeleteLogItems", myPostString, true)
            validateSuccess response

        else // Other User Roles
            let responseLogs = getAdminLogsAPIs (defaultHostLoginInfo, "GetLogItems?logType=*&pageSize=1000&pageIndex=0", false)
            let samplesLogs = JsonValue.Parse(responseLogs |> getBody)
            let totalLogCnt = samplesLogs.GetProperty("TotalResults").AsInteger()
            if totalLogCnt > 0 then // If any item exists
                let testId =  samplesLogs.GetProperty("Results").[0].GetProperty("LogGUID").AsString()
                let maxCnt = if totalLogCnt > 1000 then 1000 else totalLogCnt
                let arrayLogs = [| for i in 1..maxCnt -> "\""+samplesLogs.GetProperty("Results").[i-1].GetProperty("LogGUID").AsString()+"\"" |]
                let logIDs = arrayLogs |> Array.ofSeq |> String.concat ","
                myPostString <- myPostString.Replace("LogItemGuIdsReplaceMe", logIDs)
            else
                myPostString <- myPostString.Replace("LogItemGuIdsReplaceMe", "")

            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = updateAdminLogsAPIs (myLoginUserInfo, "DeleteLogItems", myPostString, true)
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsClear testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Delete Log Items-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = updateAdminLogsAPIs (myLoginUserInfo, "ClearLog", "", true)
        match testRole with
        | APIRoleName.HOSTUSER ->
            validateSuccess response
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsKeepMostRecentOptionsGet testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Get Keep Most Recent Options-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getAdminLogsAPIs (myLoginUserInfo, "GetKeepMostRecentOptions", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 11
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsOccurrenceOptionsGet testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Get Occurence Options-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getAdminLogsAPIs (myLoginUserInfo, "GetOccurrenceOptions", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER  ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 1
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsLogSettingsGet testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Get Log Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getAdminLogsAPIs (myLoginUserInfo, "GetLogSettings", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER  ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 1
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsLogSettingByIdGet testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Get Log Setting by Id-"+testRole.ToString() @@@ fun _ ->
        let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response1 = getAdminLogsAPIs (loginInfo, "GetLogSettings", false)
        let samples1 = JsonValue.Parse(response1 |> getBody)
        let rtnValue1 = samples1.GetProperty("TotalResults").AsInteger()
        if rtnValue1 > 0 then
            let myID = samples1.GetProperty("Results").[0].GetProperty("ID").AsString()
            let samples2 = samples1.GetProperty("Results").[0]
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = getAdminLogsAPIs (myLoginUserInfo, "GetLogSettings?logTypeConfigId="+myID, true)
            //printfn "response is %A" response
            match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let samples = JsonValue.Parse(response |> getBody)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.AreEqual true rtnValue
                let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
                Check.GreaterOrEqual rtnValue2 1
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private AdminLogsLogTypeGet testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Get Log Type-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getAdminLogsAPIs (myLoginUserInfo, "GetLogTypes", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 150
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsLogSettingAdd testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Add Log Settings-"+testRole.ToString() @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = updateAdminLogsAPIs (myLoginUserInfo, "AddLogSetting", SamplePostAddLogSettings, true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER  ->
            Check.AreEqual 200 response.statusCode
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private AdminLogsLogSettingUpdate testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Update Log Settings-"+testRole.ToString() @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response1 = getAdminLogsAPIs (defaultHostLoginInfo, "GetLogSettings", false)
        let samples1 = JsonValue.Parse(response1 |> getBody)
        let rtnValue1 = samples1.GetProperty("TotalResults").AsInteger()
        if rtnValue1 > 0 then
            let myID = samples1.GetProperty("Results").[0].GetProperty("ID").AsString()
            let samples2 = samples1.GetProperty("Results").[0]

            let mutable myPostString = SamplePostUpdateLogSettings
            myPostString <- myPostString.Replace("LogSettingIDReplaceMe", samples2.GetProperty("ID").AsString())
            myPostString <- myPostString.Replace("LogTypeFriendlyNameReplaceMe", samples2.GetProperty("LogTypeFriendlyName").AsString())
            myPostString <- myPostString.Replace("LogTypeKeyReplaceMe", samples2.GetProperty("LogTypeKey").AsString())
            myPostString <- myPostString.Replace("LogTypePortalIDReplaceMe", samples2.GetProperty("LogTypePortalID").AsString())
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = updateAdminLogsAPIs (myLoginUserInfo, "UpdateLogSetting", myPostString, true)
            //printfn "response is %A" response
            match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
                let samples = JsonValue.Parse(response |> getBody)
                let rtnValue = samples.GetProperty("ID").AsString()
                Check.AreEqual (samples2.GetProperty("ID").AsString()) rtnValue
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private AdminLogsLogSettingDelete testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Delete Log Settings-"+testRole.ToString() @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response1 = getAdminLogsAPIs (defaultHostLoginInfo, "GetLogSettings", false)
        //printfn "response is %A" response
        Check.AreEqual 200 response1.statusCode
        let samples1 = JsonValue.Parse(response1 |> getBody).GetProperty("Results")

        let mutable myPostString = ""

        // Clear Log
        updateAdminLogsAPIs (defaultHostLoginInfo, "ClearLog", "", false) |> ignore

        // Delete Log Items
        let response3 = getAdminLogsAPIs (defaultHostLoginInfo, "GetLogItems?logType=*&pageSize=1000&pageIndex=0", false)
        let samples3 = JsonValue.Parse(response3 |> getBody)
        if samples3.GetProperty("TotalResults").AsInteger() > 0 then // If any item exists
            let testId =  samples3.GetProperty("Results").[0].GetProperty("LogGUID").AsString()
            let array3 = [| for i in 1..samples3.GetProperty("TotalResults").AsInteger() -> "\""+samples3.GetProperty("Results").[i-1].GetProperty("LogGUID").AsString()+"\"" |]
            let logIDs = array3 |> Array.ofSeq |> String.concat ","

            myPostString <-  SamplePostDeleteLogItems
            myPostString <- myPostString.Replace("LogItemGuIdsReplaceMe", logIDs)
            updateAdminLogsAPIs (defaultHostLoginInfo, "DeleteLogItems", myPostString, true) |> ignore

        // Now we can delete Log Settings
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        myPostString <- SamplePostDeleteLogSettings
        myPostString <- myPostString.Replace("LogSettingIDReplaceMe", samples1.[0].GetProperty("ID").AsString())
        let response = updateAdminLogsAPIs (myLoginUserInfo, "DeleteLogSetting", myPostString, true)
        match testRole with
        | APIRoleName.HOSTUSER  ->
            validateSuccess response
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

        if response.statusCode = 200 then
            // Re-organize the values and Add it back
            myPostString <- SamplePostAddLogSettingsTemplate
            myPostString <- myPostString.Replace("LogTypeKeyReplaceMe", samples1.[0].GetProperty("LogTypeKey").AsString())
            myPostString <- myPostString.Replace("LogTypePortalIDReplaceMe", samples1.[0].GetProperty("LogTypePortalID").AsString())

            let response2 = updateAdminLogsAPIs (defaultHostLoginInfo, "AddLogSetting", myPostString, true)
            //printfn "response is %A" response
            Check.AreEqual 200 response2.statusCode
            let samples2 = JsonValue.Parse(response2 |> getBody)
            let rtnValue2 = samples2.GetProperty("ID").AsString().AsInteger()
            Check.GreaterOrEqual rtnValue2 1

let private AdminLogsLogItemsEmail testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Email Log Items-"+testRole.ToString() @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let mutable myPostString = ""

        // Get Log Items
        let response3 = getAdminLogsAPIs (defaultHostLoginInfo, "GetLogItems?logType=*&pageSize=10&pageIndex=0", false)
        let samples3 = JsonValue.Parse(response3 |> getBody)
        if samples3.GetProperty("TotalResults").AsInteger() > 0 then // If any item exists
            let testId =  samples3.GetProperty("Results").[0].GetProperty("LogGUID").AsString()
            let mutable maxCnt = samples3.GetProperty("TotalResults").AsInteger()
            if maxCnt > 10 then //pagesize = 10
                maxCnt <- 10
            let array3 = [| for i in 1..maxCnt -> "\""+samples3.GetProperty("Results").[i-1].GetProperty("LogGUID").AsString()+"\"" |]
            let logIDs = array3 |> Array.ofSeq |> String.concat ","
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            myPostString <-  SamplePostEmailLogItems
            myPostString <- myPostString.Replace("LogItemGuIdsReplaceMe", logIDs)
            let response = updateAdminLogsAPIs (myLoginUserInfo, "EmailLogItems", myPostString, true)
            match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                let samples = JsonValue.Parse(response |> getBody)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                if rtnValue then
                    Check.AreEqual true rtnValue
                else
                    Check.AreEqual
                        "There is a problem with the configuration of your SMTP Server..."
                        (samples.GetProperty("ReturnMessage").AsString())
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SchedulerServersGet testRole =
    context "WebAPI BVT"
    "WebAPI AdminLogs Get Servers-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getSchedulerAPIs (myLoginUserInfo, "GetServers", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER  ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 1
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SchedulerGetScheduleItems testRole =
    context "WebAPI BVT"
    "WebAPI Scheduler Get Schedule Items-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiGetScheduleItems (myLoginUserInfo, "?", true)
        //printfn "response is %A" response
        match testRole with
        | APIRoleName.HOSTUSER    ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("Results").AsArray()
            Check.GreaterOrEqual (rtnValue2.GetUpperBound(0)) 1
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private PortalLocalesGet testRole =
    context "WebAPI BVT"
    "WebAPI Get Portal Locales-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getPortalsAPIs (myLoginUserInfo, "GetPortalLocales?portalId=0", true)

        match testRole with
        | APIRoleName.HOSTUSER ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let rtnValue = samples.GetProperty("Success").AsBoolean()
            Check.AreEqual true rtnValue
            let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
            Check.GreaterOrEqual rtnValue2 1
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private SitesGetPortalTemplates testRole =
    context "WebAPI BVT"
    "WebAPI Get Portal Templates-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getPortalsAPIs (myLoginUserInfo, "GetPortalTemplates", true)

        // Validation
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
                let samples = JsonValue.Parse(response |> getBody)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.AreEqual true rtnValue
                let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
                Check.GreaterOrEqual rtnValue2 1
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SitesExportPortalTemplate testRole =
    context "WebAPI BVT"
    "WebAPI Export Portal Template - Current Portal -"+testRole.ToString()  @@@ fun _ ->
        // Find out a portal "0", and achieve some basic pages/tabs information
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        // Get all Tab info in an Array for site "0"
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let responseTab = apiTabsGetPortalTabs(defaultHostLoginInfo, SiteID.ToString(), true)

        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSitesExportPortalTemplate(myLoginUserInfo, SiteID.ToString(), responseTab, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        // Validation
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
                let samples = JsonValue.Parse(response |> getBody)
                let rtnValue = samples.GetProperty("Template").GetProperty("Value").AsString().Length
                Check.GreaterOrEqual rtnValue 16
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private UsersGetUserFilters testRole =
    context "WebAPI BVT"
    "WebAPI Get User Filters-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiGetUserFilters(myLoginUserInfo, true)

        match testRole with
        | APIRoleName.HOSTUSER  ->
            Check.AreEqual 200 response.statusCode
            let rtnValues = JsonValue.Parse(response |> getBody).AsArray()
            Check.AreEqual 4 (rtnValues.GetUpperBound(0))
        | APIRoleName.ADMINISTRATORS ->
            Check.AreEqual 200 response.statusCode
            let rtnValues = JsonValue.Parse(response |> getBody).AsArray()
            Check.GreaterOrEqual (rtnValues.GetUpperBound(0)) 2
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private UsersGetUsers testRole =
    context "WebAPI BVT"
    "WebAPI Get Users -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole

        let response = apiGetUserAny (myLoginUserInfo, myLoginUserInfo.UserName, "", 0, true)
        match testRole with
        | APIRoleName.HOSTUSER  ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let resultRcd = samples.GetProperty("Results").AsArray()
            Check.AreEqual -1 (resultRcd.GetUpperBound(0))
        | APIRoleName.ADMINISTRATORS ->
            Check.AreEqual 200 response.statusCode
            let samples = JsonValue.Parse(response |> getBody)
            let resultRcd = samples.GetProperty("Results").AsArray()
            Check.GreaterOrEqual (resultRcd.GetUpperBound(0)) 0
        | _ ->
            Check.AreEqual 401 response.statusCode
            //{"Message":"Authorization has been denied for this request."}

let private UsersGetUserDetail testRole =
    context "WebAPI BVT"
    "WebAPI Get User Detail - User-Self -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole

        let responseUserInfo = apiGetUserAny (myLoginUserInfo, myLoginUserInfo.UserName, "", 0, true)

        if responseUserInfo.statusCode = 200 then
            let samplesUser = JsonValue.Parse(responseUserInfo |> getBody)
            if testRole <> APIRoleName.HOSTUSER then
                let resultUserID = samplesUser.GetProperty("Results").[0].GetProperty("userId").AsString()
                let response = apiGetUserDetail (myLoginUserInfo, resultUserID, true)
                match testRole with
                     | APIRoleName.HOSTUSER  ->
                        Check.AreEqual 200 response.statusCode
                        let samples = JsonValue.Parse(response |> getBody)
                        let resultRcd = samples.GetProperty("Results").AsArray()
                        Check.AreEqual -1 (resultRcd.GetUpperBound(0))
                     | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                        let samples = JsonValue.Parse(response |> getBody)
                        let resultRcd = samples.GetProperty("userName").AsString()
                        Check.AreEqual myLoginUserInfo.UserName resultRcd
                     | _ ->
                        Check.AreEqual 401 response.statusCode
                        //{"Message":"Authorization has been denied for this request."}
        else  // For Anonymous User Only
            let resultUserID = ""
            let response = apiGetUserDetail (myLoginUserInfo, resultUserID, true)
            Check.AreEqual 401 response.statusCode

    "WebAPI Get User Detail - another RU -"+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, true)
        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = apiGetUserDetail (myLoginUserInfo, createdUserId, true)
            match testRole with
                 | APIRoleName.HOSTUSER  ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd = samples.GetProperty("userName").AsString()
                    Check.AreEqual newUserInfo.UserName resultRcd
                 | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd = samples.GetProperty("userName").AsString()
                    Check.AreEqual newUserInfo.UserName resultRcd
                 | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private UsersGetSuggestRoles testRole =
    context "WebAPI BVT"
    "WebAPI Get Suggest Roles -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole

        let response = apiGetUserSuggestRoles (myLoginUserInfo, "", true)
        match testRole with
             | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let samples = JsonValue.Parse(response |> getBody)
                let resultRcd = samples.AsArray()
                Check.AreEqual -1 (resultRcd.GetUpperBound(0))
             | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let samples = JsonValue.Parse(response |> getBody)
                let resultRcd = samples.AsArray()
                Check.GreaterOrEqual (resultRcd.GetUpperBound(0)) 0
             | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

    "WebAPI Get Suggest Roles with keyword ad -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiGetUserSuggestRoles (myLoginUserInfo, "ad", true)
        match testRole with
             | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let samples = JsonValue.Parse(response |> getBody)
                let resultRcd = samples.AsArray()
                Check.GreaterOrEqual  (resultRcd.GetUpperBound(0)) 0
             | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let samples = JsonValue.Parse(response |> getBody)
                let resultRcd = samples.AsArray()
                Check.GreaterOrEqual (resultRcd.GetUpperBound(0)) 0
             | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private UsersUpdateUserBasicInfo testRole =
    context "WebAPI BVT"
    "WebAPI Update User Basic Info -"+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, true)
        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let modifiedUserInfo = {
                newUserInfo with
                    UserID = createdUserId
                    UserName = "chged" + newUserInfo.UserName
                    EmailAddress = "chged" + newUserInfo.EmailAddress
                    DisplayName = "chged" + newUserInfo.DisplayName
                }

            use response = apiUpdateUserBasicInfo (myLoginUserInfo, modifiedUserInfo, true)
            match testRole with
                 | APIRoleName.HOSTUSER ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("userName").AsString()
                    Check.AreEqual modifiedUserInfo.UserName resultRcd
                 | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("userName").AsString()
                    Check.AreEqual modifiedUserInfo.UserName resultRcd
                 | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private UsersChangePassword testRole =
    context "WebAPI BVT"
    "WebAPI Users Change Password - RU Others -"+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, true)

        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = apiChangeUserPassword (myLoginUserInfo, createdUserId, "MyNewPassw@rd12", true)
            match testRole with
                 | APIRoleName.HOSTUSER ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private UsersForceChangePassword testRole =
    context "WebAPI BVT"
    "WebAPI Users Force Change Password - RU - "+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, true)

        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = apiForceChangeUserPassword (myLoginUserInfo, createdUserId, true)
            match testRole with
                 | APIRoleName.HOSTUSER ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private UsersSendPasswordResetLink testRole =
    context "WebAPI BVT"
    "WebAPI Users Send Password Reset Link - "+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, true)

        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = apiSendPasswordResetLink (myLoginUserInfo, createdUserId, true)
            match testRole with
                 | APIRoleName.HOSTUSER ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private UsersUpdateAuthorizeStatus testRole =
    context "WebAPI BVT"
    "WebAPI Users Update Authorize Status - "+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, true)
        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response1 = apiUpdateAuthorizeStatus (myLoginUserInfo, createdUserId, "false", true)
            let response = apiUpdateAuthorizeStatus (myLoginUserInfo, createdUserId, "true", true)
            match testRole with
                 | APIRoleName.HOSTUSER ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private UsersSoftDeleteUser testRole =
    context "WebAPI BVT"
    "WebAPI Users Soft Delete User - "+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, true)
        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response = apiSoftDeleteUser (myLoginUserInfo, createdUserId, true)
            match testRole with
                 | APIRoleName.HOSTUSER ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                    let samples = JsonValue.Parse(response |> getBody)
                    let resultRcd =  samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true resultRcd
                 | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private UsersRestoreDeletedUser testRole =
    context "WebAPI BVT"
    "WebAPI Users Restore Deleted User - "+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, false)
        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let softDeletedUser = apiSoftDeleteUser (defaultHostLoginInfo, createdUserId, false)
            let sampleUserDeleted =  JsonValue.Parse(softDeletedUser |> getBody)
            if sampleUserDeleted.GetProperty("Success").AsBoolean() then

                let myLoginUserInfo = BVTLogInDataPreparation testRole
                let response = apiRestoreDeletedUser (myLoginUserInfo, createdUserId, true)
                match testRole with
                     | APIRoleName.HOSTUSER ->
                        Check.AreEqual 200 response.statusCode
                        let samples = JsonValue.Parse(response |> getBody)
                        let resultRcd =  samples.GetProperty("Success").AsBoolean()
                        Check.AreEqual true resultRcd
                     | APIRoleName.ADMINISTRATORS  ->
                        Check.AreEqual 200 response.statusCode
                        let samples = JsonValue.Parse(response |> getBody)
                        let resultRcd =  samples.GetProperty("Success").AsBoolean()
                        Check.AreEqual true resultRcd
                     | _ ->
                        Check.AreEqual 401 response.statusCode
                        //{"Message":"Authorization has been denied for this request."}

let private UsersHardDeleteUser testRole =
    context "WebAPI BVT"
    "WebAPI Users Hard Delete User - "+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, false)
        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            let softDeletedUser = apiSoftDeleteUser (defaultHostLoginInfo, createdUserId, false)
            let sampleUserDeleted =  JsonValue.Parse(softDeletedUser |> getBody)
            if sampleUserDeleted.GetProperty("Success").AsBoolean() then
                let myLoginUserInfo = BVTLogInDataPreparation testRole
                let response = apiHardDeleteUser (myLoginUserInfo, createdUserId, true)
                match testRole with
                     | APIRoleName.HOSTUSER ->
                        Check.AreEqual 200 response.statusCode
                        let samples = JsonValue.Parse(response |> getBody)
                        let resultRcd =  samples.GetProperty("Success").AsBoolean()
                        Check.AreEqual true resultRcd
                     | APIRoleName.ADMINISTRATORS  ->
                        Check.AreEqual 200 response.statusCode
                        let samples = JsonValue.Parse(response |> getBody)
                        let resultRcd =  samples.GetProperty("Success").AsBoolean()
                        Check.AreEqual true resultRcd
                     | _ ->
                        Check.AreEqual 401 response.statusCode
                        //{"Message":"Authorization has been denied for this request."}

let private UsersUpdateSuperUserStatus testRole =
    context "WebAPI BVT"
    "WebAPI Users Update SuperUser Status - "+testRole.ToString()  @@@ fun _ ->
        let userName = "Test" + System.Guid.NewGuid().ToString()
        let newUserInfo : APICreateUserInfo =
                {
                    //UserID = "0"
                    FirstName = userName
                    LastName = "DnnTester"
                    UserName = userName
                    Password = config.Site.DefaultPassword
                    EmailAddress = userName + "DnnTester@mailinator.com"
                    DisplayName = userName
                    UserID = "0"
                    Authorize = "true"
                }
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        use createdUser = apiCreateUser (defaultHostLoginInfo, newUserInfo, false)
        if createdUser.statusCode = 200 then
            let sampleUserCreated = JsonValue.Parse(createdUser |> getBody)
            let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
            if createdUserId.AsInteger() >= 1 then
                let myLoginUserInfo = BVTLogInDataPreparation testRole
                let response = apiUpdateSuperUserStatus (myLoginUserInfo, createdUserId, "true", true)
                match testRole with
                     | APIRoleName.HOSTUSER ->
                        Check.AreEqual 200 response.statusCode
                        let samples = JsonValue.Parse(response |> getBody)
                        let resultRcd =  samples.GetProperty("Success").AsBoolean()
                        Check.AreEqual true resultRcd
                     | _ ->
                        Check.AreEqual 401 response.statusCode
                        //{"Message":"Authorization has been denied for this request."}

let private UsersCreateUser testRole =
    context "WebAPI BVT"
    "WebAPI Post Create User-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let randomStr = System.Guid.NewGuid().ToString()
        let firstName = "FN" + randomStr.Substring(0, 5)
        let lastName = "LN" + randomStr.Substring(25, 5)
        let newUserInfo : APICreateUserInfo =
            {
              FirstName = firstName
              LastName = lastName
              UserName = firstName + lastName
              Password = "dnnhost"
              EmailAddress = firstName + lastName + "@dnntest.com"
              DisplayName = firstName + lastName
              UserID = "0"
              Authorize = "true"
            }
        let response = apiCreateUser(myLoginUserInfo, newUserInfo, true)

        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let rtnUserId = JsonValue.Parse(response |> getBody).GetProperty("userId").AsInteger()
                Check.GreaterOrEqual rtnUserId 3
             | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private UsersCreateUserBatch _ =
    context "WebAPI BVT"
    "WebAPI Post Create User in Batch"  @@@ fun _ ->
        //BVTLogInDataPreparation {LogInUserRole = "HostUser"}
        let response = apiCreateUsersBatch(defaultHostLoginInfo, "QATester", 50, true)
        Check.AreEqual 50 response

let private PagesSaveBulkPages testRole =
    context "WebAPI BVT"
    "WebAPI Post Create Multiple Pages-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        //BVTLogInDataPreparation {LogInUserRole = "HostUser"}
        //(loginInfo:UserLoginInfo, bulkString:string, withPublish:bool, visible:bool, softDeleted:bool, withLog:bool)
        let response = apiSaveBulkPages(myLoginUserInfo, "",  1, "", "", "", true, true, false, true)

        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let rtnPages = JsonValue.Parse(body).GetProperty("Response").GetProperty("pages").AsArray()
                Check.GreaterOrEqual (rtnPages.GetUpperBound(0)) 1
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private VocabulariesGet testRole =
    context "WebAPI BVT"
    "WebAPI Get All Vocabularies-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = getVocabularyAPIs(myLoginUserInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", true)

        // Validation
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                    let body = response |> getBody
                    let samples = JsonValue.Parse(body)
                    let rtnValue = samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true rtnValue
                    let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
                    Check.GreaterOrEqual rtnValue2 1
                | _ ->
                    Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private VocabulariesGetTermsByID testRole =
    context "WebAPI BVT"
    "WebAPI Get Terms by Vocabulary ID-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response = getVocabularyAPIs(defaultHostLoginInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", true)
        let body = response |> getBody
        let samples = JsonValue.Parse(body)
        let rtnValue = samples.GetProperty("TotalResults").AsInteger()
        if rtnValue >= 1 then
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let myVocID = samples.GetProperty("Results").[0].GetProperty("VocabularyId").AsString()
            let response2 = getVocabularyAPIs(myLoginUserInfo, "GetTermsByVocabularyId?vocabularyId=" + myVocID + "&pageIndex=0&pageSize=10", true)
            // Validation
            if PBPermissionRead = "1" || PBPermissionEdit = "1" then
                assertTestForPBPermission(response2, "get")
            else
                match testRole with
                    | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                        Check.AreEqual 200 response2.statusCode
                        let samples2 = JsonValue.Parse(response2 |> getBody)
                        let rtnValue2 = samples2.GetProperty("Success").AsBoolean()
                        Check.AreEqual true rtnValue2
                        let rtnValue3 = samples2.GetProperty("TotalResults").AsInteger()
                        Check.GreaterOrEqual rtnValue3 0 //Initially, no terms
                    | _ ->
                        Check.AreEqual 401 response2.statusCode
                    //{"Message":"Authorization has been denied for this request."}
        else
            Check.GreaterOrEqual rtnValue 1

let private VocabulariesGetTerm testRole =
    context "WebAPI BVT"
    "WebAPI Get Terms by Vocabulary ID-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response2 = getVocabularyAPIs(myLoginUserInfo, "GetTerm?termId={termId}", true)
        // Validation
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response2, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response2.statusCode
                    let samples2 = JsonValue.Parse(response2 |> getBody)
                    let rtnValue2 = samples2.GetProperty("Success").AsBoolean()
                    Check.AreEqual true rtnValue2
                    let rtnValue3 = samples2.GetProperty("TotalResults").AsInteger()
                    Check.GreaterOrEqual rtnValue3 0 //Initially, no terms
                | _ ->
                    Check.AreEqual 401 response2.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private VocabulariesCreateTerm testRole =
    context "WebAPI BVT"
    "WebAPI Create Term-"+testRole.ToString()  @@@ fun _ ->
        printfn "  TC Executed>isChildSiteContext is equal to: %A" useChildPortal
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let mutable postString = SamplePostVocabularyTermCreation
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response = getVocabularyAPIs(defaultHostLoginInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", true)
        let body = response |> getBody
        let samples = JsonValue.Parse(body)

        let rtnValue = samples.GetProperty("TotalResults").AsInteger()
        if rtnValue >= 1 then
            //("Results").[0] returns: "VocabularyId": 1, "Name": "Tags", "Description": "System Vocabulary for free form user entered Tags", "Type": "Simple",
            //"TypeId": 1, "ScopeType": "Application", "ScopeTypeId": 1, "IsSystem": true
            let mutable iIndex = 0
            // Need to find IsSystem = False for Admin Account; otherwise admin user will get 401 error
            while samples.GetProperty("Results").[iIndex].GetProperty("IsSystem").AsBoolean() && iIndex <= rtnValue do
                iIndex <- iIndex + 1
            let myVocID = samples.GetProperty("Results").[iIndex].GetProperty("VocabularyId").AsString()
            let myRandomStr = System.Guid.NewGuid().ToString()
            postString <- postString.Replace ("VocabularyIdReplaceMe", myVocID)
            postString <- postString.Replace ("TermNameReplaceMe", "Name-" + myRandomStr)
            postString <- postString.Replace ("TermDescriptionReplaceMe", "Description-" + myRandomStr)
            postString <- postString.Replace ("ParentTermIdReplaceMe", "-1")
            // Create Term
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
            let response2 = postVocabularyAPIs(myLoginUserInfo, "CreateTerm", postString, true)

            // Validation
            if PBPermissionRead = "1" || PBPermissionEdit = "1" then
                assertTestForPBPermission(response2, "post")
            else

                match testRole with
                    | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                        Check.AreEqual 200 response2.statusCode
                        let samples2 = JsonValue.Parse(response2 |> getBody)
                        let rtnValue2 = samples2.GetProperty("Success").AsBoolean()
                        Check.AreEqual true rtnValue2
                    | _ ->
                        Check.AreEqual 401 response2.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private VocabulariesUpdateTerm testRole =
    context "WebAPI BVT"
    "WebAPI Update Term-"+testRole.ToString()  @@@ fun _ ->
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        // Step1: get Terms by Vocabulary ID
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response = getVocabularyAPIs(defaultHostLoginInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", false)
        let body = response |> getBody
        let samples = JsonValue.Parse(body)
        let myVocID = samples.GetProperty("Results").[0].GetProperty("VocabularyId").AsString()
        let rtnValue = samples.GetProperty("TotalResults").AsInteger()
        let mutable rtnTerm = response
        if rtnValue >= 1 then
            let mutable iIndex = 0
            // Need to find IsSystem = False for Admin Account; otherwise admin user will get 401 error
            while samples.GetProperty("Results").[iIndex].GetProperty("IsSystem").AsBoolean() && iIndex <= rtnValue do
                iIndex <- iIndex + 1

            // Step 1a: get the first Vocabulary ID
            let myVocID = samples.GetProperty("Results").[iIndex].GetProperty("VocabularyId").AsString()
            // Step 1b: get the Terms by that Vocabulary ID
            let response2 = getVocabularyAPIs(defaultHostLoginInfo, "GetTermsByVocabularyId?vocabularyId=" + myVocID + "&pageIndex=0&pageSize=10", true)
            let samples2 = JsonValue.Parse(response2 |> getBody)
            let rtnValue2 = samples2.GetProperty("Success").AsBoolean()
            let termsCount = samples2.GetProperty("TotalResults").AsInteger()
            rtnTerm <- postVocabularyCreateTerm (defaultHostLoginInfo, myVocID, false)
            let samples3 = JsonValue.Parse(rtnTerm |> getBody)
            let termID =  samples3.GetProperty("TermId").AsString()
            let response4 = getVocabularyAPIs(defaultHostLoginInfo, "GetTerm?termId=" + termID , false)
            let samples4 = JsonValue.Parse(response4 |> getBody)
            let mutable parentItemId = ""
            match samples4.GetProperty("ParentTermId").ToString() with
                | "null" -> parentItemId <- "-1"
                | _ -> parentItemId <- samples4.GetProperty("ParentTermId").AsString()

            // Update Term
            let mutable updString = SamplePostVocabularyTermUpdate
            updString <- updString.Replace("TermIdReplaceMe", termID)
            updString <- updString.Replace ("ParentIdReplaceMe", parentItemId)
            updString <- updString.Replace ("VocabularyIdReplaceMe", samples4.GetProperty("VocabularyId").AsString())
            updString <- updString.Replace ("TermNameReplaceMe", samples4.GetProperty("Name").AsString())
            updString <- updString.Replace ("TermDescriptionReplaceMe", "Description changed")
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response5= postVocabularyAPIs (myLoginUserInfo, "UpdateTerm", updString, true)
            if PBPermissionRead = "1" || PBPermissionEdit = "1" then
                assertTestForPBPermission(response5, "post")
            else
                match testRole with
                    | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                        Check.AreEqual 200 response5.statusCode
                        let samples5 = JsonValue.Parse(response5 |> getBody)
                        let rtnValue5 = samples5.GetProperty("Success").AsBoolean()
                        Check.AreEqual true rtnValue5
                    | _ ->
                        Check.AreEqual 401 response5.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private VocabularyCreate testRole  =
    context "WebAPI BVT"
    "WebAPI Create Vocabulary-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole

        let mutable postString = SamplePostVocabulary
        let myRandomStr = System.Guid.NewGuid().ToString()
        postString <- postString.Replace ("vocabularyNameReplaceMe", "Voc-"+myRandomStr)
        postString <- postString.Replace ("DescriptionReplaceMe", "Description Of " + "Voc-"+myRandomStr)
        postString <- postString.Replace ("ScopeTypeIdReplaceMe", "1")
        postString <- postString.Replace ("TypeIdReplaceMe", "1")

        // Create vocabulary
        let response = postVocabularyAPIs(myLoginUserInfo, "CreateVocabulary", postString, true)

        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response.statusCode
                    let body = response |> getBody
                    let samples = JsonValue.Parse(body)
                    let rtnValue = samples.GetProperty("Success").AsBoolean()
                    Check.AreEqual true rtnValue
                | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private VocabularyUpdate testRole  =
    context "WebAPI BVT"
    "WebAPI Update Vocabulary-"+testRole.ToString()  @@@ fun _ ->
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        //We don't have getVocabularyById, but only getVocabularies
        //I will pick up first item from getVocabularies
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response = getVocabularyAPIs(defaultHostLoginInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", true)
        let body = response |> getBody
        let samples = JsonValue.Parse(body)
        let rtnValue = samples.GetProperty("TotalResults").AsInteger()
        let mutable postString = ""
        if rtnValue >= 1 then
            //("Results").[0] returns: "VocabularyId": 1, "Name": "Tags", "Description": "System Vocabulary for free form user entered Tags", "Type": "Simple",
            //"TypeId": 1, "ScopeType": "Application", "ScopeTypeId": 1, "IsSystem": true
            let mutable iIndex = 0
            // Need to find IsSystem = False for Admin Account; otherwise admin user will get 401 error
            while samples.GetProperty("Results").[iIndex].GetProperty("IsSystem").AsBoolean() && iIndex <= rtnValue do
                iIndex <- iIndex + 1

            let myVocID = samples.GetProperty("Results").[iIndex].GetProperty("VocabularyId").AsString()
            postString <- SamplePostVocabularyUpdate
            postString <- postString.Replace ("vocabularyIdReplaceMe", myVocID)
            postString <- postString.Replace ("vocabularyNameReplaceMe", samples.GetProperty("Results").[iIndex].GetProperty("Name").AsString())
            postString <- postString.Replace ("DescriptionChanged", "Description Changed")
            postString <- postString.Replace ("VocTypeIdReplaceMe", samples.GetProperty("Results").[iIndex].GetProperty("TypeId").AsString())
            postString <- postString.Replace ("VocTypeReplaceMe", samples.GetProperty("Results").[iIndex].GetProperty("Type").AsString())
            postString <- postString.Replace ("ScopeTypeIdReplaceMe", samples.GetProperty("Results").[iIndex].GetProperty("ScopeTypeId").AsString())
            postString <- postString.Replace ("ScopeTypeReplaceMe", samples.GetProperty("Results").[iIndex].GetProperty("ScopeType").AsString())
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let response2 = postVocabularyAPIs(myLoginUserInfo, "UpdateVocabulary", postString, true)
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                    Check.AreEqual 200 response2.statusCode
                    let samples2 = JsonValue.Parse(response2 |> getBody)
                    let rtnValue2 = samples2.GetProperty("Success").AsBoolean()
                    Check.AreEqual true rtnValue2
                | _ ->
                Check.AreEqual 401 response2.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SitesGetPortals testRole  =
    context "WebAPI BVT"
    "WebAPI Get Portals-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSitesGetPortals(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        if PBPermissionSiteInfoView = "1" || PBPermissionSiteInfoEdit = "1" then
            assertTestForPBPermissionSiteInfo(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER ->
                    Check.AreEqual 200 response.statusCode
                    let body = response |> getBody
                    let samples = JsonValue.Parse(body)
                    let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
                    Check.GreaterOrEqual rtnValue2 1
                 | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private TabsGetPortalTabs testRole  =
    context "WebAPI BVT"
    "WebAPI Get Portal Tabs-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiTabsGetPortalTabs(myLoginUserInfo, portalID, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.AreEqual true rtnValue
                let rtnValue2 = samples.GetProperty("Results").GetProperty("ChildTabs").AsArray().GetUpperBound(0)
                Check.GreaterOrEqual rtnValue2 1
             | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private TabsGetTabsDESCENDANTS testRole  =
    context "WebAPI BVT"
    "WebAPI Get Tabs DESCENDANTS-"+testRole.ToString()  @@@ fun _ ->
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = SiteID.ToString()
        let responseTabs = apiTabsGetPortalTabs(defaultHostLoginInfo, portalID, true)
        let arrayTabs = JsonValue.Parse(responseTabs |> getBody).GetProperty("Results").GetProperty("ChildTabs").AsArray()
        let tabsArrayWithChild = arrayTabs
                                    |> Array.filter (fun myElem -> myElem.GetProperty("HasChildren").AsBoolean())
        let tabIdWithChild = tabsArrayWithChild.[0].GetProperty("TabId").AsString() // Get the first available Parent Tab ID

        // Log in as required User Role
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        let response = apiTabsGetTabsDescendants(myLoginUserInfo, portalID, tabIdWithChild, true)

        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.AreEqual true rtnValue
                let rtnValue2 = samples.GetProperty("Results").AsArray().GetUpperBound(0)
                Check.GreaterOrEqual rtnValue2 1
             | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SitesGetPortalLocales testRole  =
    context "WebAPI BVT"
    "WebAPI Get Protal Locales-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSitesGetPortalLocales(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
      //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.AreEqual true rtnValue
                let rtnValue2 = samples.GetProperty("TotalResults").AsInteger()
                Check.GreaterOrEqual rtnValue2 1
             | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SitesGetRequiresQandA testRole =
    context "WebAPI BVT"
    "WebAPI Get Protal Locales-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSitesGetRequiresQuestionAndAnswer(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SitesCreatePortal testRole =
    context "WebAPI BVT"
    "WebAPI Create Portal-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole

        //We don't have getVocabularyById, but only getVocabularies
        //I will pick up first item from getVocabularies
        let response = apiSitesCreateChildPortal(myLoginUserInfo, "", "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Portal").GetProperty("PortalID").AsInteger ()
                Check.GreaterOrEqual rtnValue 1
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SitesDeletePortal testRole =
    context "WebAPI BVT"
    "WebAPI Delete Portal-"+testRole.ToString()  @@@ fun _ ->
        //Prepare a child site by Host
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response1 = apiSitesCreateChildPortal(defaultHostLoginInfo, "", "", true)
        let samples = JsonValue.Parse(response1 |> getBody)
        let rtnValue = samples.GetProperty("Portal").GetProperty("PortalID").ToString()

        //Delete this Site based on the user role
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSitesDeleteChildPortal(myLoginUserInfo, rtnValue, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.AreEqual true rtnValue
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SitesDeleteExpiredPortals testRole =
    context "WebAPI BVT"
    "WebAPI Delete Expired Portals-"+testRole.ToString()  @@@ fun _ ->
        //Delete expired Sites based on the user role
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSitesDeleteExpiredPortals(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.AreEqual true rtnValue
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private SitesCreateRole testRole =
    context "WebAPI BVT"
    "WebAPI Create Role-"+testRole.ToString()  @@@ fun _ ->
        //BVTLogInDataPreparation {LogInUserRole = "HostUser"}SitesGetPortals

        //logOff()
        //let loginInfo = apiLogin config.Site.HostUserName config.Site.DefaultPassword
        let userAvailable = BVTLogInDataPreparation testRole
        let response = apiRolesCreateNewRole(userAvailable, true)
        Check.AreEqual 200 response.statusCode
        let body = response |> getBody
        let samples = JsonValue.Parse(body)
        let rtnValue = samples.GetProperty("id").AsInteger()
        Check.GreaterOrEqual rtnValue 4
        apiRolesCreateNewRoles(userAvailable, 20, true)

let private SiteInfoGetPortalSettings testRole =
    context "WebAPI BVT"
    "WebAPI Site Info - Get Portal Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSiteInfoGetPortalSettings(myLoginUserInfo, "", "", true)
        if PBPermissionSiteInfoView = "1" || PBPermissionSiteInfoEdit = "1" then
            assertTestForPBPermissionSiteInfo(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                        let body = response |> getBody
                        let samples = JsonValue.Parse(body)
                        // Validate Settings > GUID exists
                        let rtnValue = samples.GetProperty("Settings").GetProperty("GUID").AsString().Length
                        Check.GreaterOrEqual rtnValue 6
                        let rtnValue = samples.GetProperty("TimeZones").AsArray().GetUpperBound(0)
                        Check.GreaterOrEqual rtnValue 100
                | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private SiteInfoUpdatePortalSettings testRole =
    context "WebAPI BVT"
    "WebAPI Site Info - Update Portal Settings-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let response = apiSiteInfoGetPortalSettings(defaultHostLoginInfo, "","", true)
        let body = response |> getBody
        let samples = JsonValue.Parse(body)

        let mutable myPostString = SamplePostPortalSettings
        myPostString <- myPostString.Replace ("PortalNameReplaceMe", samples.GetProperty("Settings").GetProperty("PortalName").AsString())
        myPostString <- myPostString.Replace ("GUIDReplaceMe", samples.GetProperty("Settings").GetProperty("GUID").AsString())
        myPostString <- myPostString.Replace ("HomeDirectoryReplaceMe", samples.GetProperty("Settings").GetProperty("HomeDirectory").AsString())

        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSiteInfoUpdatePortalSettings(myLoginUserInfo, myPostString, true)

        if PBPermissionSiteInfoView = "1" || PBPermissionSiteInfoEdit = "1" then
            assertTestForPBPermissionSiteInfo(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                | _ ->
                    Check.AreEqual 401 response.statusCode
                    //{"Message":"Authorization has been denied for this request."}

let private ThemesGetThemes testRole =
    context "WebAPI BVT"
    "WebAPI Get Themes-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesGetThemes(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private ThemesGetThemeFiles testRole =
    context "WebAPI BVT"
    "WebAPI Get Theme Files-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let themeResponse = apiThemesGetCurrentTheme(defaultHostLoginInfo, true)
        let themeBody = themeResponse |> getBody
        let themeSamples = JsonValue.Parse(themeBody)
        let themeName = themeSamples.GetProperty("SiteLayout").GetProperty("themeName").AsString()

        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesGetThemeFiles(myLoginUserInfo, themeName, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                //let body = response |> getBody
                //let samples = JsonValue.Parse(body)
                //let rtnValue = samples.GetProperty("Layouts").AsArray().GetUpperBound(0)
                //Check.GreaterOrEqual rtnValue 4
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private ThemesGetCurrentTheme testRole =
    context "WebAPI BVT"
    "WebAPI Get Current Theme-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesGetCurrentTheme(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
          //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("SiteLayout").GetProperty("themeName").AsString()
                Check.GreaterOrEqual rtnValue.Length 1
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private ThemesGetEditableTokens testRole =
    context "WebAPI BVT"
    "WebAPI Get Editable Tokens-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesGetEditableTokens(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.AsArray().GetUpperBound(0)
                Check.GreaterOrEqual rtnValue 0
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ThemesGetEditableSettings testRole =
    context "WebAPI BVT"
    "WebAPI Get Editable Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesGetEditableSettings(myLoginUserInfo, "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.AsArray().GetUpperBound(0)
                Check.GreaterOrEqual rtnValue 1
            | _ ->
                Check.AreEqual 401 response.statusCode

    "WebAPI Get Editable Settings All-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let tokenResponse = apiThemesGetEditableTokens(defaultHostLoginInfo, true)
        let tokenBody = tokenResponse |> getBody
        let tokenArray = JsonValue.Parse(tokenBody).AsArray()
        let mutable tokenValue = ""
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        for i in 0 .. tokenArray.GetUpperBound(0) do
            tokenValue <- tokenArray.[i].GetProperty("value").AsString().Replace("/", "%2F")
            let response = apiThemesGetEditableSettings(myLoginUserInfo, tokenValue, true)
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
                | _ ->
                    Check.AreEqual 401 response.statusCode

let private ThemesGetEditableValues testRole =
    context "WebAPI BVT"
    "WebAPI Get Editable Values-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesGetEditableValues(myLoginUserInfo,"", "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
            | _ ->
                Check.AreEqual 401 response.statusCode

    "WebAPI Get Editable Values ALL-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let tokenResponse = apiThemesGetEditableTokens(defaultHostLoginInfo, true)
        let tokenBody = tokenResponse |> getBody
        let tokenArray = JsonValue.Parse(tokenBody).AsArray()
        let mutable tokenSettingValue = ""
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        for i in 0 .. tokenArray.GetUpperBound(0) do
            tokenSettingValue <- tokenArray.[i].GetProperty("value").AsString().Replace("/", "%2F")
            let tokenSettingsResponse = apiThemesGetEditableSettings(myLoginUserInfo, tokenSettingValue, true)
            let tokenSettingsBody = tokenSettingsResponse |> getBody
            let valueArray = JsonValue.Parse(tokenSettingsBody).AsArray()
            for j in 0 .. valueArray.GetUpperBound(0) do
                let response = apiThemesGetEditableValues(myLoginUserInfo, tokenSettingValue, valueArray.[j].GetProperty("value").AsString() , true)
                match testRole with
                    | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                    | _ ->
                        Check.AreEqual 401 response.statusCode

let private ThemesParseTheme testRole =
    context "WebAPI BVT"
    "WebAPI Post Parse Theme-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesParseTheme(myLoginUserInfo, "0", "Xcillion", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private ThemesDeleteTheme testRole =
    context "WebAPI BVT"
    "WebAPI Get Themes-"+testRole.ToString()  @@@ fun _ ->
        //{name: "Home", type: 0, path: "[G]/Cavalier/Home.ascx" }
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesDeleteTheme(myLoginUserInfo, "", "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private ThemesApplyDefaultTheme testRole =
    context "WebAPI BVT"
    "WebAPI Apply Default Theme-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiThemesApplyDefaultTheme(myLoginUserInfo, "", true) //use default value: Gravity / Xcillion(platform)
        // Change back.
        //let response2 = apiThemesApplyDefaultTheme(myLoginUserInfo, "Cavalier", true)
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

let private ThemesApplyTheme testRole =
    context "WebAPI BVT"
    "WebAPI Apply Theme-"+testRole.ToString()  @@@ fun _ ->
        //let loginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let themesResponse = apiThemesGetThemes(defaultHostLoginInfo, true)
        let themesBody = themesResponse |> getBody
        let themeSamples = JsonValue.Parse(themesBody)
        let themeNames = themeSamples.GetProperty("Layouts").AsArray()
        let myThemeName = "Xcillion"
        let myLayout = "home"
        let themeFileResponses = apiThemesGetThemeFiles(defaultHostLoginInfo, myThemeName, true)
        let themeFilesSamples = JsonValue.Parse(themeFileResponses |> getBody)
        let themeFilesSamples2 = themeFilesSamples.AsArray()
        //Validation by user role
        //{scope:1,themeFile:{canDelete:false,name:"popupskin",path:"[G]skins/cavalier/popupskin",themeName:"Cavalier",thumbnail:null,type:0}}
        let themeScope = "1" //1 - Site Skin  2 - Edit Skin  3 - Site & Edit Skin
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response1 = apiThemesApplyTheme(myLoginUserInfo, themeScope, themeFilesSamples2, "popupskin", true)
        let response = apiThemesApplyTheme(myLoginUserInfo, themeScope, themeFilesSamples2, myLayout, true)

        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                //let body = response |> getBody
                //let samples = JsonValue.Parse(body)
                //let rtnValue = samples.GetProperty("Layouts").AsArray().GetUpperBound(0)
                //Check.GreaterOrEqual rtnValue 4
            | _ ->
                Check.AreEqual 401 response.statusCode
                //{"Message":"Authorization has been denied for this request."}

(*///////////////////////////////////// Dnn.PersonaBar.Seo /////////////////////////////////////////
  //////https://dnntracker.atlassian.net/wiki/display/DP/Dnn.PersonaBar.Seo+-+Request+examples//////*)
let private SeoGetGeneralSettings testRole =
    context "WebAPI BVT"
    "WebAPI SEO Get General Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoGetGeneralSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoUpdateGeneralSettings testRole =
    context "WebAPI BVT"
    "WebAPI SEO Update General Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoUpdateGeneralSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoGetRegexSettings testRole =
    context "WebAPI BVT"
    "WebAPI SEO Get RegEx Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoGetRegexSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoUpdateRegexSettings testRole =
    context "WebAPI BVT"
    "WebAPI SEO Update RegEx Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoUpdateRegexSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoGetSitemapSettings testRole =
    context "WebAPI BVT"
    "WebAPI SEO Get Sitemap Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoGetSitemapSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoUpdateSitemapSettings testRole =
    context "WebAPI BVT"
    "WebAPI SEO Update Sitemap Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoUpdateSitemapSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

// expired
let private SeoGetProviders testRole =
    context "WebAPI BVT"
    "WebAPI SEO Get Providers -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoGetProviders(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoGetExtensionUrlProviders testRole =
    context "WebAPI BVT"
    "WebAPI SEO Get ExtensionUrl Providers -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoGetExtensionUrlProviders(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoUpdateSiteMapProviders testRole =
    context "WebAPI BVT"
    "WebAPI SEO Update SiteMap Providers -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoUpdateSiteMapProviders(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoSitemapResetCache testRole =
    context "WebAPI BVT"
    "WebAPI SEO Reset Cache -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoSitemapResetCache(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoCreateVerification testRole =
    context "WebAPI BVT"
    "WebAPI SEO Create Virification -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoCreateVerification(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoTestURL testRole =
    context "WebAPI BVT"
    "WebAPI SEO Test URL -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoTestURL(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private SeoTestURLRewriter testRole =
    context "WebAPI BVT"
    "WebAPI SEO Test URL Rewriter -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiSeoTestURLRewriter(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsString()
                Check.GreaterOrEqual rtnValue "true"
            | _ ->
                Check.AreEqual 401 response.statusCode

let private RolesGetRoles testRole =
    context "WebAPI BVT"
    "WebAPI Roles Get Roles -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiRolesGetRoles(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnRolesArray = samples.GetProperty("roles").AsArray()
                Check.GreaterOrEqual (rtnRolesArray.GetUpperBound(0)) 3
            | _ ->
                Check.AreEqual 401 response.statusCode

let private RolesCreateRole testRole =

    context "WebAPI BVT"
    "WebAPI Roles Create Role-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiRolesSaveRole(myLoginUserInfo, "",0, true)   // 0 means get a random number.
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("id").AsInteger()
                Check.GreaterOrEqual rtnValue 1
            | _ ->
                Check.AreEqual 401 response.statusCode

    "WebAPI Roles Create Roles (Load)-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole

        for i in 1..9 do
            apiRolesSaveRole(myLoginUserInfo, "",i, true) |> ignore  // 0 means get a random number.

        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName

let private ExtensionsParsePackage testRole =

    context "WebAPI BVT"
    "WebAPI Extensions Parse Package-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiExtensionsParsePackage(myLoginUserInfo, true)   // 0 means get a random number.
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("id").AsInteger()
                Check.GreaterOrEqual rtnValue 1
            | _ ->
                Check.AreEqual 401 response.statusCode

// Expired?
let private ServersGetWebServerHostSettings testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get WebServer Host Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetWebServerHostSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let servers = samples.GetProperty("servers").AsArray()
                Check.GreaterOrEqual (servers.GetUpperBound(0)) 1
                //let serverID = servers.[0].GetProperty("ServerID").AsString()
                //let serverName = servers.[0].GetProperty("ServerName").AsString()
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersGetWebServersInfo testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get WebServers Info-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetWebServersInfo(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        Check.AreEqual 404 response.statusCode

let private ServersGetSMTPSettings testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get SMTP Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetSMTPSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let smtpInfo = samples.GetProperty("smtpServerMode").AsString()
                Check.GreaterOrEqual smtpInfo.Length 1
                //let serverID = servers.[0].GetProperty("ServerID").AsString()
                //let serverName = servers.[0].GetProperty("ServerName").AsString()
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersGetPerformanceSettings testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get Performance Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetPerformanceSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnInfo = samples.GetProperty("CachingProviderOptions").AsArray()
                Check.GreaterOrEqual (rtnInfo.GetUpperBound(0)) 0
                //let serverID = servers.[0].GetProperty("ServerID").AsString()
                //let serverName = servers.[0].GetProperty("ServerName").AsString()
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersGetLogSettings testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get Log Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetLogSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.AreEqual true rtnValue
                let rtnInfo = samples.GetProperty("Results").GetProperty("LogList").AsArray()
                Check.GreaterOrEqual (rtnInfo.GetUpperBound(0)) 0
                //let serverID = servers.[0].GetProperty("ServerID").AsString()
                //let serverName = servers.[0].GetProperty("ServerName").AsString()
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersGetLogFile testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get Log File-"+testRole.ToString()  @@@ fun _ ->
        let myHostLoginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let responseLogs = apiServersGetLogSettings(myHostLoginInfo, false)
        let samples = JsonValue.Parse(responseLogs |> getBody)
        let rtnLogs = samples.GetProperty("Results").GetProperty("LogList").AsArray()
        if rtnLogs.GetUpperBound(0) >= 0 then
            let logFileName = rtnLogs.[0].AsString()
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
            let response = apiServersGetLogFile(myLoginUserInfo, logFileName, false)
            //Validation by Product and user role
            match testRole with
                | APIRoleName.HOSTUSER  ->
                    Check.AreEqual 200 response.statusCode
                | _ ->
                    Check.AreEqual 401 response.statusCode

let private ServersGetAppInfo testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get Application Info-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetAppInfo(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("product").AsString().ToLower()
                Check.Contains "platform" (rtnValue.ToLower())
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersGetDBInfo testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get DB Info-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetDBInfo(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("productVersion").AsString()
                Check.GreaterOrEqual rtnValue.Length 4
                //let AppGUID = samples.GetProperty("guid").AsString()
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersClearCache testRole =
    context "WebAPI BVT"
    "WebAPI Servers Clear Cache-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersClearCache(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                //let body = response |> getBody
                //let samples = JsonValue.Parse(body)
                //let rtnValue = samples.GetProperty("productVersion").AsString()
                //Check.GreaterOrEqual(rtnValue.Length, 4)
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersRestartApplication testRole =
    context "WebAPI BVT"
    "WebAPI Servers Restart Application-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersRestartApplication(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersSendTestEmail testRole =
    context "WebAPI BVT"
    "WebAPI Servers Send Test Email-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersSendTestEmail(myLoginUserInfo, "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                if response.statusCode = 400 then
                    let body = response |> getBody
                    let samples = JsonValue.Parse(body)
                    // without a SMTP server service or a dummy one like Papercut, it always return false
                    let rtnValue = samples.GetProperty("success").AsBoolean()
                    Check.GreaterOrEqual rtnValue false
                else
                    Check.AreEqual 200 response.statusCode

            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersUpdateSMTPSettings testRole =
    context "WebAPI BVT"
    "WebAPI Servers Update SMTP Settings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersUpdateSMTPSettings(myLoginUserInfo, "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("success").AsBoolean()
                Check.GreaterOrEqual rtnValue true
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersIncreaseHostVersion testRole =
    context "WebAPI BVT"
    "WebAPI Servers Increase Host Version-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersIncreaseHostVersion(myLoginUserInfo,  true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.GreaterOrEqual rtnValue true
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersUpdatePerformanceSettings testRole =
    context "WebAPI BVT"
    "WebAPI Servers Increase Host Version-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersUpdatePerformanceSettings(myLoginUserInfo, "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        match testRole with
            | APIRoleName.HOSTUSER  ->
                Check.AreEqual 200 response.statusCode
                let body = response |> getBody
                let samples = JsonValue.Parse(body)
                let rtnValue = samples.GetProperty("Success").AsBoolean()
                Check.GreaterOrEqual rtnValue true
            | _ ->
                Check.AreEqual 401 response.statusCode

let private ServersGetCachedItemList testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get Cached Item List-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetCachedItemList(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by Product and user role
        Check.AreEqual 404 response.statusCode

let private ServersGetCachedItem testRole =
    context "WebAPI BVT"
    "WebAPI Servers Get Cached Item-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        //let myLocalHostLoginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER
        let responseList = apiServersGetCachedItemList(defaultHostLoginInfo, false)
        let samples = JsonValue.Parse(responseList |> getBody)
        Check.AreEqual 404 responseList.statusCode
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let response = apiServersGetCachedItem (myLoginUserInfo, "", true)
        Check.AreEqual 404 response.statusCode

let private PagesGetDefaultSettings testRole  =
    context "WebAPI BVT"
    "WebAPI Get Pages GetDefaultSettings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesGetDefaultSettings(myLoginUserInfo, portalID, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS -> Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

    "WebAPI Get Pages GetDefaultSettings-ChildPortalOnly"+testRole.ToString()  @@@ fun _ ->
        if useChildPortal then // during ChildPortal Mode
            let myLoginUserInfo = BVTLogInDataPreparation testRole
            let portalID = setAPIPortalId().ToString()
            let response = apiPagesGetDefaultSettings(myLoginUserInfo, portalID, true)
            printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
            //Validation by user role
            // Only Host can get portal 0 data
            match testRole with
                | APIRoleName.HOSTUSER  -> Check.AreEqual 200 response.statusCode
                | _ -> Check.AreEqual 401 response.statusCode

let private PagesPostSavePageDetails testRole  =
    context "WebAPI BVT"
    "WebAPI Post Pages SavePageDetails-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSavePageDetails(myLoginUserInfo, portalID, "", true)
        let body = response |> getBody
        let sample = JsonValue.Parse(body)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
                    let pageId = sample.GetProperty("Page").GetProperty("id").AsInteger()
                    Check.Greater pageId 10
            | _ -> Check.AreEqual 401 response.statusCode

    "WebAPI Post Pages SavePageDetails-Won't change TabOrder-"+testRole.ToString()  @@@ fun _ ->
        // searc page: Activity Feed > Friends
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()

        let response = apiPagesGetPageList(defaultHostLoginInfo, portalID, "Friend", true)
        //let response = apiRLAddCard(defaultHostLoginInfo, portalID, "Friend", true)
        let body = response |> getBody
        let sample = JsonValue.Parse(body)

        if sample.AsArray().GetUpperBound(0) >= 0 then
            let pageId = sample.[0].GetProperty("id").AsString()
            let tabOrder = sample.[0].GetProperty("tabOrder").AsString()

            let response = apiPagesUpdatePageDetails(defaultHostLoginInfo, portalID, pageId, "level", "1", true)
            let body = JsonValue.Parse(response |> getBody)
            let tabOrder2 = body.GetProperty("Page").GetProperty("tabOrder").AsString()
            printfn "  TC Executed by User: %A" testRole
            if response.statusCode <= 300 then
                Check.AreEqual tabOrder tabOrder

let private TemplatesPostSavePageDetails testRole  =
    context "WebAPI BVT"
    "WebAPI Post Templates SavePageDetails-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiTemplatesGetPageTemplates(defaultHostLoginInfo, portalID, false)
        let templateParentId = JsonValue.Parse(response |> getBody).[0].GetProperty("id").AsInteger()

        let response = apiTemplatesSavePageDetails(myLoginUserInfo, portalID, "", templateParentId.ToString(), true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        // Validation
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                        let body = response |> getBody
                        let pageId = JsonValue.Parse(body).GetProperty("Page").GetProperty("id").AsInteger()
                        Check.Greater pageId 10
                | _ -> Check.AreEqual 401 response.statusCode

let private TemplatesPostSavePagePermissions testRole  =
    context "WebAPI BVT"
    "WebAPI Post Templates SavePagePermissions-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = setAPIPortalId().ToString()
        let response = apiTemplatesGetPageTemplates(defaultHostLoginInfo, portalID, false)
        let body = response |> getBody
        let templateParentId = JsonValue.Parse(body).[0].GetProperty("id").AsInteger()

        let response = apiTemplatesSavePageDetails(defaultHostLoginInfo, portalID, "", templateParentId.ToString(), true)
        let body = response |> getBody
        let templateParentId = JsonValue.Parse(body).GetProperty("Page").GetProperty("id").AsInteger()
        let response = apiTemplatesSavePagePermissions(myLoginUserInfo, templateParentId.ToString(), true)

        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        // Validation
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                        let body = response |> getBody
                        let pageId = JsonValue.Parse(body).GetProperty("Page").GetProperty("id").AsInteger()
                        Check.Greater pageId 10
                | _ -> Check.AreEqual 401 response.statusCode

let private TemplatesPostEditModeForPage testRole  =
    context "WebAPI BVT"
    "WebAPI Post Templates Edit Mode for Page -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = setAPIPortalId().ToString()
        let response = apiTemplatesGetPageTemplates(defaultHostLoginInfo, portalID, false)
        let body = response |> getBody
        let templateParentId = JsonValue.Parse(body).[0].GetProperty("id").AsInteger()
        let response = apiTemplatesEditModeForPage(myLoginUserInfo, templateParentId.ToString(), true)

        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        // Validation
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                        let body = response |> getBody
                        let pageId = JsonValue.Parse(body).GetProperty("Page").GetProperty("id").AsInteger()
                        Check.Greater pageId 10
                | _ -> Check.AreEqual 401 response.statusCode

let private TemplatesPostDeletePage testRole  =
    context "WebAPI BVT"
    "WebAPI Post Templates Delete Page -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiTemplatesGetPageTemplates(defaultHostLoginInfo, portalID, false)
        let body = response |> getBody
        let templateParentId = JsonValue.Parse(body).[0].GetProperty("id").AsInteger()
        let response = apiTemplatesSavePageDetails(defaultHostLoginInfo, portalID, "", templateParentId.ToString(), true)
        let body = response |> getBody
        let templateParentId = JsonValue.Parse(body).GetProperty("Page").GetProperty("id").AsInteger()
        let response = apiTemplatesDeletePage(myLoginUserInfo, templateParentId.ToString(), true)

        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        // Validation
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                        let pageId = JsonValue.Parse(response |> getBody).GetProperty("Page").GetProperty("id").AsInteger()
                        Check.Greater pageId 10
                | _ -> Check.AreEqual 401 response.statusCode

let private ISPostToggleUserMode testRole  =
    context "WebAPI BVT"
    "WebAPI Post InternalService ToggleUserMode-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSavePageDetails(myLoginUserInfo, portalID, "", true)
        let body = response |> getBody
        Check.AreEqual 200 response.statusCode
        let pageId = JsonValue.Parse(body).GetProperty("Page").GetProperty("id").AsString()
        let response = apiISToggleUserMode(myLoginUserInfo, pageId, "EDIT", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

let private ISGetPortalDesktopModules testRole  =
    context "WebAPI BVT"
    "WebAPI Get InternalService PortalDesktopModules ALL-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiISGetPortalDesktopModulesAll(myLoginUserInfo, true)
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

    "WebAPI Get InternalService PortalDesktopModules SearchTerm-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiISGetPortalDesktopModulesAny(myLoginUserInfo, "Journal", true)
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

let private ISPostAddAModule testRole =
    context "WebAPI BVT"
    "WebAPI Post InternalService AddModule-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()

        // Get a new page
        let response = apiPagesSavePageDetails(defaultHostLoginInfo, portalID, "", true)
        let body = response |> getBody
        let pageId = JsonValue.Parse(body).GetProperty("Page").GetProperty("id").AsString()
        // Get a module
        let response = apiISGetPortalDesktopModulesAny(defaultHostLoginInfo, "Journal", true)
        let body = response |> getBody
        let moduleId = JsonValue.Parse(body).[0].GetProperty("ModuleID").AsString()
        let response = apiISPostAddModule(myLoginUserInfo, pageId, moduleId, true)

        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

// Shall be replaced by Toggle User Mode
let private PagesGetEditModeForPage testRole  =
    context "WebAPI BVT"
    "WebAPI POST Pages EditModeForPage-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSearchPages(defaultHostLoginInfo, portalID, "Main", true)
        let body = response |> getBody
        let sample = JsonValue.Parse(body)
        let pageID = sample.GetProperty("Results").[0].GetProperty("id").AsString()

        let myLoginUserInfo = BVTLogInDataPreparation testRole

        let response = apiPagesEditModeForPage(myLoginUserInfo, portalID, pageID,  true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

let private PagesSearchPages testRole =
    context "WebAPI BVT"
    "WebAPI Get Pages SearchPages empty string -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSearchPages(myLoginUserInfo, portalID, "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let pages = sample.GetProperty("Results").AsArray()
            Check.GreaterOrEqual pages.Length 1

    "WebAPI Get Pages SearchPages w SearchString -"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSearchPages(myLoginUserInfo, portalID, "searchKey=&pageType=&tags=&lastModifiedOnStartDate=&lastModifiedOnEndDate=&pageIndex=0&pageSize=100", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let pages = sample.GetProperty("Results").AsArray()
            Check.GreaterOrEqual pages.Length 1

    "WebAPI Get Pages SearchPages w SearchString PublishDate-None-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSearchPages(myLoginUserInfo, portalID, "searchKey=&pageType=&tags=&publishDateStart=07/18/2017&publishDateEnd=07/18/2017&pageIndex=0&pageSize=100", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let pages = sample.GetProperty("Results").AsArray()
            Check.AreEqual 0 pages.Length

    "WebAPI Get Pages SearchPages w SearchString PublishDate-All-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSearchPages(myLoginUserInfo, portalID, "searchKey=&pageType=&tags=&publishDateStart=07/18/2015 00:00:00 AM&publishDateEnd=07/18/2099 23:59:59 PM&pageIndex=0&pageSize=100", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let pages = sample.GetProperty("Results").AsArray()
            Check.GreaterOrEqual pages.Length 1

    "WebAPI Get Pages SearchPages w SearchString PublishStatus-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSearchPages(myLoginUserInfo, portalID, "searchKey=&pageType=&tags=&publishDateStart=&publishDateEnd=&pageIndex=0&pageSize=100&publishStatus=published", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let pages = sample.GetProperty("Results").AsArray()
            Check.GreaterOrEqual pages.Length 1

    "WebAPI Get Pages SearchPages w SearchString PageIndexSize-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiPagesSearchPages(myLoginUserInfo, portalID, "searchKey=&pageType=&tags=&pageIndex=2&pageSize=1", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode
        if response.statusCode = 200 then
            let body = response |> getBody
            let sample = JsonValue.Parse(body)
            let pages = sample.GetProperty("Results").AsArray()
            Check.AreEqual 1 pages.Length

let private SiteExportImportGetAllJobs testRole  =
    context "WebAPI BVT"
    "WebAPI GET SiteExportImport AllJobs-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiSiteEIGetAllJobs(myLoginUserInfo, portalID, "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER   ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

let private SiteExportImportPostExport testRole  =
    context "WebAPI BVT"
    "WebAPI Post SiteExportImport ExportAll-"+testRole.ToString()  @@@ fun _ ->
        defaultHostLoginInfo <- BVTLogInDataPreparation APIRoleName.HOSTUSER
        let portalID = setAPIPortalId().ToString()

        let myLoginUserInfo = BVTLogInDataPreparation testRole
        //let response = apiSitesCreateChildPortal(myLoginUserInfo, "", "", true)
        let response = apiTabsGetPortalTabs(defaultHostLoginInfo, portalID, false)
        let pageSelection = getSiteExportImportPagesString(response, 0)

        let response = apiSiteEIPostExport(myLoginUserInfo, portalID, pageSelection, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER   ->
                    Check.AreEqual 200 response.statusCode
                    let scheduleId = apiFindScheduleItemByName (defaultHostLoginInfo, "Site Import/Export", false)
                    if scheduleId > 0 then
                        let response = apiPostRunSchedule(defaultHostLoginInfo, scheduleId.ToString(), false)
                        Check.AreEqual 200 response.statusCode

            | _ -> Check.AreEqual 401 response.statusCode

let private ComponentsGetSuggestionRoles testRole  =
    context "WebAPI BVT"
    "WebAPI GET Components GetSuggestionRoles-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiComponentsGetSuggestionRoles(myLoginUserInfo, "-1", "trans", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

    "WebAPI GET Components GetSuggestionRoles Appending/Disbled Role-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let newRole = apiRolesCreateNewRoleAny(defaultHostLoginInfo, "-1", "-1", false)
        let newRoleName = JsonValue.Parse(newRole |> getBody).GetProperty("name").AsString()
        let response = apiComponentsGetSuggestionRoles(myLoginUserInfo, "-1", newRoleName, true)
        printfn "  TC Executed by User: %A, with appending role" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
                    let body = response |> getBody
                    let newSuggestionRoleList = JsonValue.Parse(body).AsArray()
                    Check.AreEqual -1 (newSuggestionRoleList.GetUpperBound(0))
            | _ -> Check.AreEqual 401 response.statusCode

        let newRole = apiRolesCreateNewRoleAny(defaultHostLoginInfo, "-1", "0", false)
        let newRoleName = JsonValue.Parse(newRole |> getBody).GetProperty("name").AsString()
        let response = apiComponentsGetSuggestionRoles(myLoginUserInfo, "-1", newRoleName, true)
        printfn "  TC Executed by User: %A, with disabled role" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
                    let body = response |> getBody
                    let newSuggestionRoleList = JsonValue.Parse(body).AsArray()
                    Check.AreEqual -1 (newSuggestionRoleList.GetUpperBound(0))
            | _ -> Check.AreEqual 401 response.statusCode

let private ComponentsGetSuggestionUsers testRole  =
    context "WebAPI BVT"
    "WebAPI GET Components GetSuggestionUsers-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        //let portalID =
        let response = apiComponentsGetSuggestionUsers(myLoginUserInfo, "auto", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
            | _ -> Check.AreEqual 401 response.statusCode

    "WebAPI GET Components GetSuggestionUsers Appending/Disbled User-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let randomStr = System.Guid.NewGuid().ToString()
        let firstName = "FN" + randomStr.Substring(0, 5)
        let lastName = "LN" + randomStr.Substring(25, 5)
        let newUserInfo : APICreateUserInfo =
            {
              FirstName = firstName
              LastName = lastName
              UserName = firstName + lastName
              Password = "dnnhost"
              EmailAddress = firstName + lastName + "@dnntest.com"
              DisplayName = firstName + lastName
              UserID = "0"
              Authorize = "true"
            }
        let newUser = apiCreateUserAny(defaultHostLoginInfo, newUserInfo, false, false, false) //authorize = false
        let newUserBody = JsonValue.Parse(newUser |> getBody)
        let newUserName = newUserBody.GetProperty("userName").AsString()
        let response = apiComponentsGetSuggestionUsers(myLoginUserInfo, newUserName, true)
        printfn "  TC Executed by User: %A, The NewUser: %A is a Non Authorized User" myLoginUserInfo.UserName newUserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
                    let body = response |> getBody
                    let newSuggestionUserList = JsonValue.Parse(body).AsArray()
                    Check.AreEqual -1 (newSuggestionUserList.GetUpperBound(0))
            | _ -> Check.AreEqual 401 response.statusCode

        let randomStr = System.Guid.NewGuid().ToString()
        let firstName = "FN" + randomStr.Substring(0, 5)
        let lastName = "LN" + randomStr.Substring(25, 5)
        let newUserInfo : APICreateUserInfo =
            {
              FirstName = firstName
              LastName = lastName
              UserName = firstName + lastName
              Password = "dnnhost"
              EmailAddress = firstName + lastName + "@dnntest.com"
              DisplayName = firstName + lastName
              UserID = "0"
              Authorize = "true"
            }
        let newUser = apiCreateUserAny(defaultHostLoginInfo, newUserInfo, true, true, false) //deleted = true; authorize = true,
        let newUserBody = JsonValue.Parse(newUser |> getBody)
        if newUser.statusCode<=201 then
            let newUserId = newUserBody.GetProperty("userId").AsString()
            apiSoftDeleteUser (defaultHostLoginInfo, newUserId, false) |> ignore

        let newUserName = newUserBody.GetProperty("userName").AsString()
        let response = apiComponentsGetSuggestionUsers(myLoginUserInfo, newUserName, true)
        printfn "  TC Executed by User: %A, The NewUser: %A is a Deleted user" myLoginUserInfo.UserName newUserName
        //Validation by user role
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                    Check.AreEqual 200 response.statusCode
                    let body = response |> getBody
                    let newSuggestionUserList = JsonValue.Parse(body).AsArray()
                    Check.AreEqual -1 (newSuggestionUserList.GetUpperBound(0))
            | _ -> Check.AreEqual 401 response.statusCode

let private securityGetBasicLoginSettings testRole  =
    context "WebAPI BVT"
    "WebAPI GET Security GetBasicLoginSettings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiSecurityGetBasicLoginSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                | _ -> Check.AreEqual 401 response.statusCode

let private securityGetIPFilter testRole  =
    context "WebAPI BVT"
    "WebAPI GET Security GetIPFilter-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiSecurityGetIPFilter(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                | _ -> Check.AreEqual 401 response.statusCode

let private securityGetMemberSettings testRole  =
    context "WebAPI BVT"
    "WebAPI GET Security GetMemberSettings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiSecurityGetMemberSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                | _ -> Check.AreEqual 401 response.statusCode

let private securityGetRegistrationSettings testRole  =
    context "WebAPI BVT"
    "WebAPI GET Security GetRegistrationSettings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiSecurityGetRegistrationSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "get")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                | _ -> Check.AreEqual 401 response.statusCode

let private securityUpdateBasicLoginSettings testRole  =
    context "WebAPI BVT"
    "WebAPI POST Security UpdateBasicLoginSettings-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let response = apiSecurityUpdateBasicLoginSettings(myLoginUserInfo, true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        //Validation by user role
        if PBPermissionRead = "1" || PBPermissionEdit = "1" then
            assertTestForPBPermission(response, "post")
        else
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                        Check.AreEqual 200 response.statusCode
                | _ -> Check.AreEqual 401 response.statusCode

let private extensionsGetAvailablePackages testRole  =
    context "WebAPI BVT"
    "WebAPI GET Extensions GetAvailablePackages-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        let extensionPackageTypes = [|"CoreLanguagePack"; "Auth_System"; "Library"; "Provider"; "Module"; "Connector"; "Container"; "ExtensionLanguagePack"; "JavaScript_Library"; "PersonaBar"; "Skin"; "SkinObject"; "Widget"|]

        for iType in extensionPackageTypes do
            printfn "  Loop Extensions GetAvailablePackages for Type: %A" iType
            let response = apiGetExtensionsAvailablePackages (myLoginUserInfo, iType, true)
            let body = response |> getBody
            let body = JsonValue.Parse(body)
            match testRole with
                | APIRoleName.HOSTUSER   ->
                        Check.AreEqual 200 response.statusCode
                        Check.IsTrue(body.GetProperty("Success").AsBoolean())
                        let totalResults = body.GetProperty("TotalResults").AsInteger()
                        printfn "  Available Extensions for Type: %A is %A" iType totalResults
                | _ -> Check.AreEqual 401 response.statusCode

let private extensionsGetInstalledPackages testRole  =
    context "WebAPI BVT"
    "WebAPI GET Extensions GetInstalledPackages-"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
        let extensionPackageTypes = [|"CoreLanguagePack"; "Auth_System"; "Library"; "Provider"; "Module"; "Connector"; "Container"; "ExtensionLanguagePack"; "JavaScript_Library"; "PersonaBar"; "Skin"; "SkinObject"; "Widget"|]

        for iType in extensionPackageTypes do
            //printfn "  Loop Extensions GetInstalledPackages for Type: %A" iType
            let response = apiGetExtensionsAvailablePackages (myLoginUserInfo, iType, true)
            let body = response |> getBody
            let body = JsonValue.Parse(body)
            match testRole with
                | APIRoleName.HOSTUSER   ->
                        Check.AreEqual 200 response.statusCode
                        Check.IsTrue(body.GetProperty("Success").AsBoolean())
                        let totalResults = body.GetProperty("TotalResults").AsInteger()
                        printfn "  Installed Extensions for Type: %A is %A" iType totalResults
                | _ -> Check.AreEqual 401 response.statusCode

let private pbPermissionVocabularies testRole  =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Vocabulary"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let menuIdentifierName = "Dnn.Vocabularies"

        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)

        printfn "View Only"
        // Turn on the View Permission
        let response = setPBMenuPermission(defaultHostLoginInfo, portalID, responseRoleIDs, menuIdentifierName, "view", "1", true)
        let response = setPBMenuPermission(defaultHostLoginInfo, portalID, responseRoleIDs, menuIdentifierName, "edit", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        // Log in user as Registered User
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        // Validation
        let responseView = getVocabularyAPIs(myLoginUserInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", true)
        Check.AreEqual 200 responseView.statusCode
        let responseEdit = postVocabularyCreateDefault(myLoginUserInfo)
        Check.AreEqual 401 responseEdit.statusCode

        printfn "View and Edit"
        // Turn on the Edit Permission
        let response = setPBMenuPermission(defaultHostLoginInfo, portalID, responseRoleIDs, menuIdentifierName, "view", "1", true)
        let response = setPBMenuPermission(defaultHostLoginInfo, portalID, responseRoleIDs, menuIdentifierName, "edit", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        let responseView = getVocabularyAPIs(myLoginUserInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", true)
        Check.AreEqual 200 responseView.statusCode
        let responseEdit = postVocabularyCreateDefault(myLoginUserInfo)
        Check.AreEqual 200 responseEdit.statusCode

        printfn "View Only again"
        // Turn off the edit Permission
        let response = setPBMenuPermission(defaultHostLoginInfo, portalID, responseRoleIDs, menuIdentifierName, "view", "1", true)
        let response = setPBMenuPermission(defaultHostLoginInfo, portalID, responseRoleIDs, menuIdentifierName, "edit", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        let responseView = getVocabularyAPIs(myLoginUserInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", true)
        Check.AreEqual 200 responseView.statusCode
        let responseEdit = postVocabularyCreateDefault(myLoginUserInfo)
        Check.AreEqual 401 responseEdit.statusCode

        printfn "No View nor Edit"
        // Turn off the View Permission
        let response = setPBMenuPermission(defaultHostLoginInfo, portalID, responseRoleIDs, menuIdentifierName, "view", "0", true)
        let response = setPBMenuPermission(defaultHostLoginInfo, portalID, responseRoleIDs, menuIdentifierName, "edit", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        let responseView = getVocabularyAPIs(myLoginUserInfo, "GetVocabularies?pageIndex=0&pageSize=10&scopeTypeId=*", true)
        Check.AreEqual 401 responseView.statusCode
        let responseEdit = postVocabularyCreateDefault(myLoginUserInfo)
        Check.AreEqual 401 responseEdit.statusCode

// SITE_INFO is a special permission
let private pbPermissionSetSiteInfoViewEdit testRole  =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsSiteInfo (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetSiteInfoView testRole  =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsSiteInfo (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetSiteInfoNone testRole pbIdentifierName  =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsSiteInfo (defaultHostLoginInfo, portalID, responseRoleIDs, "0", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetSiteInfoEdit testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsSiteInfo (defaultHostLoginInfo, portalID, responseRoleIDs, "0", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

// Extra permission for Users
let private pbPermissionSetUsersNone testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Users Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsUsers (defaultHostLoginInfo, portalID, responseRoleIDs, "0", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetUsersView testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Users Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsUsers (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetUsersViewEdit testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Users Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsUsers (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

// Extra permission for AdminLogs
let private pbPermissionSetAdminLogsNone testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set AdminLogs Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsAdminLogs (defaultHostLoginInfo, portalID, responseRoleIDs, "0", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetAdminLogsViewEdit testRole pbIdentifierName  =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Users Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsAdminLogs (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetAdminLogsView testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Users Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsAdminLogs (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetRecycleBinViewEdit testRole pbIdentifierName  =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set RecycleBin Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsRecycleBin (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetRecycleBinView testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set RecycleBin Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsRecycleBin (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetRecycleBinNone testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set RecycleBin Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsRecycleBin (defaultHostLoginInfo, portalID, responseRoleIDs, "0", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetSecurityViewEdit testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Security Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsSecurity (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1
let private pbPermissionSetSecurityView testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Security Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsSecurity (defaultHostLoginInfo, portalID, responseRoleIDs, "1", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetSecurityNone testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set null Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissions (defaultHostLoginInfo, portalID, responseRoleIDs, pbIdentifierName, "0", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1
        validatePBMenuByIdentifierName (pbIdentifierName) |> ignore

let private pbPermissionSetView testRole pbIdentifierName  =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set View Permissions "+pbIdentifierName.ToString()+" for "+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissions (defaultHostLoginInfo, portalID, responseRoleIDs, pbIdentifierName, "1", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1
        validatePBMenuByIdentifierName (pbIdentifierName) |> ignore

let private pbPermissionSetViewEdit testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set ViewEdit Permissions "+pbIdentifierName.ToString()+" for "+testRole.ToString()   @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissions (defaultHostLoginInfo, portalID, responseRoleIDs, pbIdentifierName, "1", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetEdit testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Edit Permissions "+pbIdentifierName.ToString()+" for "+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissions (defaultHostLoginInfo, portalID, responseRoleIDs, pbIdentifierName, "0", "1", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionSetNone testRole pbIdentifierName =
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission Test-Set Permissions"+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        //Registered User
        let responseRoleIDs= getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        setPBMenuPermissionsAll (defaultHostLoginInfo, portalID, responseRoleIDs, "0", "0", true)
        apiServersClearCache(defaultHostLoginInfo, true)  |> ignore
        sleep 0.1

let private pbPermissionPageList testRole pbIdentifierName = 
    context "WebAPI BVT"
    "WebAPI PersonaBar Permission View Page List Default Permissions" + testRole.ToString() @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()

        let defaultPermissionKeyName = 
            match pbIdentifierName with
            | "Dnn.Page" -> "VIEW_PAGE_LIST"
            | _ -> "VIEW_PAGE_LIST"

        //Registered User
        let responseRoleIDs = getRoleIDByRoleName (defaultHostLoginInfo, testRole, false)
        let response = getPBMenuDefaultPermission (defaultHostLoginInfo, defaultPermissionKeyName, true)
        let body = response |> getBody
        let body = JsonValue.Parse(body)
        Check.AreEqual 200 response.statusCode
        let dataString = body.GetProperty("Data").[0].ToString()
        Check.GreaterOrEqual (dataString.IndexOf("Registered Users")) 0
        apiServersClearCache (defaultHostLoginInfo, true) |> ignore
        sleep 0.1

let private saCookieDNNPersonalizationByAnonymous testRole =
    context "WebAPI BVT Security"
    "WebAPI Security Analyzer Regression CONTENT-8414 "+testRole.ToString()  @@@ fun _ ->
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        let portalID = setAPIPortalId().ToString()
        let folderPath = "d:\mySites\social9100624qa1"
        let saCookieString = """<profile><item key="name1:key1" type="System.Data.Services.Internal.ExpandedWrapper`2[[DotNetNuke.Common.Utilities.FileSystemUtils],[System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]], System.Data.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"><ExpandedWrapperOfFileSystemUtilsObjectDataProvider xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><ExpandedElement/><ProjectedProperty0><MethodName>PullFile</MethodName><MethodParameters><anyType xsi:type="xsd:string">http://www.dnnsoftware.com/default.aspx</anyType><anyType xsi:type="xsd:string">D:\MySites\social9100624qa1\alert-defaultaspx.txt</anyType></MethodParameters><ObjectInstance xsi:type="FileSystemUtils"></ObjectInstance></ProjectedProperty0></ExpandedWrapperOfFileSystemUtilsObjectDataProvider></item></profile>"""
        let sitePath = config.Site.WebsiteFolder
        let ck = OpenQA.Selenium.Cookie("DNNPersonalization", saCookieString)
        browser.Manage().Cookies.AddCookie(ck)
        goto "/b" //goto any non-exist page
        let file = findFileInFolder folderPath "alert-defaultaspx.txt"
        Check.AreEqual file.IsNone true
        sleep 0.1

let private permissionOnly _ =
    // For debug mode, one by one
    let pbIdNameList = [
                        "Dnn.AdminLogs";
                        "Dnn.ConfigConsole";
                        "Dnn.CssEditor";
                        "Dnn.AdminLogs";
                        "Dnn.ConfigConsole";
                        "Dnn.CssEditor";
                        "Dnn.Extensions";
                        "Dnn.Licensing";
                        "Dnn.Recyclebin";
                        "Dnn.Roles";
                        "Dnn.Security";
                        "Dnn.Seo";
                        "Dnn.Servers";
                        "Dnn.SiteSettings";
                        "Dnn.Sites";
                        "Dnn.SqlConsole";
                        "Dnn.TaskScheduler";
                        "Dnn.Themes";
                        "Dnn.Users";
                        "Dnn.Vocabularies";
                        "Dashboard";
                        "Dnn.Pages";
                        "Dnn.SiteImportExport";
                        ]
    let roleList = [
                        APIRoleName.HOSTUSER
                        APIRoleName.REGISTEREDUSERS
                        APIRoleName.ANONYMOUS
                    ]

    let testCaseList = [
                            pbPermissionPageList
                            pbPermissionSetView
                            pbPermissionSetViewEdit
                            pbPermissionSetNone  // includes: Assets, vocabulary, Pages, Roles,
                            pbPermissionSetUsersNone
                            pbPermissionSetRecycleBinNone
                            pbPermissionSetAdminLogsNone
                            pbPermissionSetSecurityNone
                            pbPermissionSetSiteInfoNone
                            pbPermissionSetView
                            pbPermissionSetView  // includes: Assets, vocabulary, Pages, Roles,
                            pbPermissionSetUsersView
                            pbPermissionSetRecycleBinView
                            pbPermissionSetAdminLogsView
                            pbPermissionSetSecurityView
                            pbPermissionSetViewEdit  // includes: Assets, vocabulary, Pages, Roles,
                            pbPermissionSetUsersViewEdit
                            pbPermissionSetRecycleBinViewEdit
                            pbPermissionSetAdminLogsViewEdit
                            pbPermissionSetSecurityViewEdit
                            pbPermissionSetViewEdit
                            pbPermissionSetUsersViewEdit
                            pbPermissionSetView
                            pbPermissionSetUsersNone
                            // Templates
                            pbPermissionSetNone
                            pbPermissionSetSiteInfoNone
                            pbPermissionSetEdit
                            pbPermissionSetSiteInfoEdit
                            pbPermissionSetViewEdit
                            pbPermissionSetView
                            pbPermissionSetNone
                            pbPermissionSetSiteInfoNone
                        ]

    roleList |> List.iter bvtLogInAccountsPreSet
    for testcase in testCaseList do
        for role in roleList do
            for pbIdName in pbIdNameList do
                testcase role pbIdName

let private dataloading _ =
    let roleList = [
                    APIRoleName.HOSTUSER
                   ]

    let testCaseList = [
                          PagesSaveBulkPages
                          SitesCreatePortal
                        ]
    roleList |> List.iter bvtLogInAccountsPreSet
    roleList |> List.iter (fun role -> testCaseList |> List.iter (fun testcase -> testcase role))

let all _ =
    //let context = new NUnit.Framework.Internal.TestExecutionContext()
    //context.EstablishExecutionEnvironment()
    if 1 = 1 then
        let roleList = [
                        APIRoleName.HOSTUSER
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.REGISTEREDUSERS
                        APIRoleName.ANONYMOUS
                       ]

        let testCaseListGeneral = [
                                    SitesGetRequiresQandA
                                    SitesGetPortalLocales
                                    SitesGetPortals
                                    TabsGetPortalTabs
                                    TabsGetTabsDESCENDANTS
                                    PagesSaveBulkPages
                                    PagesGetDefaultSettings
                                    PagesPostSavePageDetails
                                    PagesGetEditModeForPage
                                    PagesSearchPages
                                    ISPostToggleUserMode
                                    ISGetPortalDesktopModules
                                    ISPostAddAModule
                                    SitesCreatePortal
                                    SitesDeletePortal
                                    SiteInfoGetPortalSettings
                                    SiteInfoUpdatePortalSettings
                                    SitesGetPortalTemplates
                                    SitesDeleteExpiredPortals
                                    SitesExportPortalTemplate
                                    VocabularyCreate
                                    VocabulariesCreateTerm
                                    VocabulariesGet
                                    VocabularyUpdate
                                    VocabulariesUpdateTerm
                                    VocabulariesGetTermsByID
                                    UsersGetUserFilters
                                    UsersGetUsers
                                    UsersGetUserDetail
                                    UsersUpdateUserBasicInfo
                                    UsersCreateUser
                                    UsersChangePassword
                                    UsersForceChangePassword
                                    UsersSendPasswordResetLink
                                    UsersUpdateAuthorizeStatus
                                    UsersSoftDeleteUser
                                    UsersRestoreDeletedUser
                                    UsersHardDeleteUser
                                    UsersUpdateSuperUserStatus
                                    PortalLocalesGet
                                    RolesGetRoles
                                    RolesCreateRole
                                    SchedulerServersGet
                                    SchedulerGetScheduleItems
                                    AdminLogsItemsGet
                                    AdminLogsItemsDelete
                                    AdminLogsPortalsGet
                                    AdminLogsLogTypeGet
                                    AdminLogsLogSettingAdd
                                    AdminLogsLogSettingsGet
                                    AdminLogsOccurrenceOptionsGet
                                    AdminLogsKeepMostRecentOptionsGet
                                    AdminLogsLogItemsEmail
                                    AdminLogsLogSettingUpdate
                                    AdminLogsLogSettingByIdGet
                                    AdminLogsLogSettingDelete
                                    AdminLogsClear
                                    ServersGetWebServersInfo
                                    ServersGetSMTPSettings
                                    ServersGetPerformanceSettings
                                    ServersGetLogSettings
                                    ServersGetAppInfo
                                    ServersGetDBInfo
                                    ServersClearCache
                                    ServersRestartApplication
                                    ServersSendTestEmail
                                    ServersUpdateSMTPSettings
                                    ServersIncreaseHostVersion
                                    ServersUpdatePerformanceSettings
                                    ServersGetLogFile
                                    ServersGetCachedItemList
                                    ServersGetCachedItem
                                    ConfigFileMerge
                                    ConfigFileGetByName
                                    ConfigFileUpdate
                                    ConfigFileListGet
                                    aqlConsoleSaveQuery
                                    aqlConsoleRunQuery
                                    CustomCSSGet
                                    CustomCSSUpdate
                                    CustomCSSRestore
                                    aqlConsoleDeleteQueryById
                                    aqlConsoleGetSavedQueryById
                                    SeoGetGeneralSettings
                                    SeoGetRegexSettings
                                    SeoGetSitemapSettings
                                    SiteMapGetProviders
                                    SeoGetExtensionUrlProviders
                                    SeoUpdateGeneralSettings
                                    SeoUpdateRegexSettings
                                    SeoUpdateSiteMapProviders
                                    SeoSitemapResetCache
                                    SeoCreateVerification
                                    ThemesGetThemes
                                    ThemesGetCurrentTheme
                                    ThemesGetThemeFiles
                                    ThemesGetEditableTokens
                                    ThemesGetEditableSettings
                                    ThemesGetEditableValues
                                    ThemesApplyTheme
                                    ThemesApplyDefaultTheme // Try to run this at the last
                                    SiteExportImportGetAllJobs //Site Export/Import
                                    SiteExportImportPostExport
                                    ComponentsGetSuggestionRoles
                                    ComponentsGetSuggestionUsers
                                    securityGetMemberSettings
                                    securityGetIPFilter
                                    securityGetRegistrationSettings
                                    promptCommandLine
                                    extensionsGetAvailablePackages
                                    extensionsGetInstalledPackages
                                   ]
        roleList |> List.iter bvtLogInAccountsPreSet
        roleList |> List.iter (fun role -> testCaseListGeneral |> List.iter (fun testcase -> testcase role))

    else
        let roleList = [
                            APIRoleName.HOSTUSER
                            APIRoleName.ADMINISTRATORS
                            APIRoleName.REGISTEREDUSERS
                            APIRoleName.ANONYMOUS
                       ]

        let testCaseList = [
                              AdminLogsItemsDelete
                              AdminLogsClear
                              extensionsGetAvailablePackages
                              extensionsGetInstalledPackages
                              AdminLogsItemsDelete
                              AdminLogsPortalsGet
                              AdminLogsLogTypeGet
                              AdminLogsLogSettingAdd
                              AdminLogsLogSettingsGet
                              AdminLogsOccurrenceOptionsGet
                              AdminLogsKeepMostRecentOptionsGet
                              AdminLogsLogItemsEmail
                              AdminLogsLogSettingUpdate
                              AdminLogsLogSettingByIdGet
                              AdminLogsLogSettingDelete
                           ]
        roleList |> List.iter bvtLogInAccountsPreSet
        roleList |> List.iter (fun role -> testCaseList |> List.iter (fun testcase -> testcase role))
