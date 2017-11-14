module SecurityRegression

open System
open canopy
open HttpClient
open DnnCanopyContext
open DnnTypes
open DnnUserLogin
open APIHelpers
open NUnit.Framework
open FSharp.Data
open WebAPIBVT

//page info
let private loginPage = "/Login"



let mutable myLoginInfo : UserLoginInfo =     { UserName = ""
                                                Password = ""
                                                DisplayName = ""
                                                DNNCookie = { name=""; value="" }
                                                RVCookie = { name=""; value="" }
                                                RVToken = { name=""; value="" }
                                                }

let arrayAdmin : UserLoginInfo array = Array.zeroCreate 4



let BVTLogInDataPreparation (roleName:APIRoleName) =

    canopy.configuration.skipRemainingTestsInContextOnFailure <- true
    let userNamePrefix = bingportalPrefix 
    match roleName  with
        | APIRoleName.ADMINISTRATORS -> 
                if myLoginInfo.UserName <> userNamePrefix+"AutoADMIN" then myLoginInfo <- apiLoginAsAdmin2 (bingportalPrefix)
        | APIRoleName.CONTENTMANAGERS -> 
                if myLoginInfo.UserName <> userNamePrefix+"AutoCM" then myLoginInfo <- apiLoginAsCM()
        | APIRoleName.CONTENTEDITORS -> 
                if myLoginInfo.UserName <> userNamePrefix+"AutoCE" then myLoginInfo <- apiLoginAsCE()
        | APIRoleName.COMMUNITYMANAGER -> 
                if myLoginInfo.UserName <> userNamePrefix+"AutoCOM" then myLoginInfo <- apiLoginAsCOM()
        | APIRoleName.MODERATORS -> 
                if myLoginInfo.UserName <> userNamePrefix+"AutoMOD" then myLoginInfo <- apiLoginAsMOD()
        | APIRoleName.HOSTUSER -> 
                if myHostLoginInfo.UserName <> config.Site.HostUserName then 
                    loginAsHost()
                    let allCookieString = browser.Manage().Cookies.AllCookies
                    let userCookie = browser.Manage().Cookies.GetCookieNamed(".DOTNETNUKE")
                    let RVCookie = browser.Manage().Cookies.GetCookieNamed("__RequestVerificationToken")
                    let myRVToken = getRequestVerificationToken(false, true)
                    myHostLoginInfo.UserName <-  config.Site.HostUserName 
                    myHostLoginInfo.Password <- config.Site.DefaultPassword 
                    myHostLoginInfo.DisplayName <- config.Site.HostDisplayName
                    myHostLoginInfo.DNNCookie <- { name=userCookie.Name; value=userCookie.Value }
                    myHostLoginInfo.RVCookie <- { name=RVCookie.Name; value=RVCookie.Value }
                    myHostLoginInfo.RVToken <- { name="RequestVerificationToken"; value=myRVToken}
                    myLoginInfo <- myHostLoginInfo
                else
                    myLoginInfo <- myHostLoginInfo
        | _ -> 
                if myLoginInfo.UserName <> userNamePrefix+"AutoTesterRU" then myLoginInfo <- apiLoginAsRU()

    canopy.configuration.skipRemainingTestsInContextOnFailure <- false
    myLoginInfo

let BVTLogInAccountsPreSetFun roleName =

        let hostUser = apiLogin config.Site.HostUserName config.Site.DefaultPassword
        let userNamePrefix = bingportalPrefix 
        //if DnnCanopyContext.isChildSiteContext then "C" else ""
        let userName = 
            match roleName  with
                | APIRoleName.HOSTUSER -> "host"  
                | APIRoleName.ADMINISTRATORS -> userNamePrefix+"AutoADMIN"            
                | APIRoleName.CONTENTMANAGERS -> userNamePrefix+"AutoCM"
                | APIRoleName.CONTENTEDITORS -> userNamePrefix+"AutoCE"
                | APIRoleName.MODERATORS -> userNamePrefix+"AutoMOD"
                | APIRoleName.COMMUNITYMANAGER -> userNamePrefix+"AutoCOM"
                | _ -> userNamePrefix+"AutoTesterRU"
        if roleName.ToString() <> "HOSTUSER" then  
            let rtnUserId = apiUsersIfUserExists (hostUser, userName)
            if rtnUserId < 0 then  // User does not exist
                let newUserInfo : APICreateUserInfo = 
                    { 
                      FirstName = roleName.ToString()
                      LastName = "DnnTester"
                      UserName = userName
                      Password = config.Site.DefaultPassword
                      EmailAddress = roleName.ToString() + "DnnTester@mailinator.com" 
                      DisplayName = roleName.ToString() + roleName.ToString()
                      UserID = "0"
                    }
                System.Threading.Thread.Sleep 1000
                let createdUser = apiCreateUser (hostUser, newUserInfo, true) 
                let sampleUserCreated = JsonValue.Parse(createdUser.EntityBody.Value)
                let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
                apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName    
                System.Threading.Thread.Sleep 2000
                //let rtnUserId2 = apiUsersIfUserExists (hostUser, userName)
                Assert.GreaterOrEqual(createdUserId.AsInteger(), 1)
            else
                Assert.GreaterOrEqual(rtnUserId, 1)
        else
            Assert.AreEqual(roleName.ToString(), "HOSTUSER")

let BVTLogInAccountsPreSet roleName =
    context "WebAPI Data Loading - PreSet Accounts (Bing)"
    (sprintf "WebAPI Prepare for Users in role %A" roleName) @@@ fun _ -> 

        let hostUser = apiLogin config.Site.HostUserName config.Site.DefaultPassword
        let userNamePrefix = bingportalPrefix 
        //if DnnCanopyContext.isChildSiteContext then "C" else ""
        let userName = 
            match roleName  with
                | APIRoleName.HOSTUSER -> "host"  
                | APIRoleName.ADMINISTRATORS -> userNamePrefix+"AutoADMIN"            
                | APIRoleName.CONTENTMANAGERS -> userNamePrefix+"AutoCM"
                | APIRoleName.CONTENTEDITORS -> userNamePrefix+"AutoCE"
                | APIRoleName.MODERATORS -> userNamePrefix+"AutoMOD"
                | APIRoleName.COMMUNITYMANAGER -> userNamePrefix+"AutoCOM"
                | _ -> userNamePrefix+"AutoTesterRU"
        if roleName.ToString() <> "HOSTUSER" then  
            let rtnUserId = apiUsersIfUserExists (hostUser, userName)
            if rtnUserId < 0 then  // User does not exist
                let newUserInfo : APICreateUserInfo = 
                    { 
                      FirstName = roleName.ToString()
                      LastName = "DnnTester"
                      UserName = userName
                      Password = config.Site.DefaultPassword
                      EmailAddress = roleName.ToString() + "DnnTester@mailinator.com" 
                      DisplayName = roleName.ToString() + roleName.ToString()
                      UserID = "0"
                    }
                System.Threading.Thread.Sleep 1000
                let createdUser = apiCreateUser (hostUser, newUserInfo, true) 
                let sampleUserCreated = JsonValue.Parse(createdUser.EntityBody.Value)
                let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
                apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName    
                System.Threading.Thread.Sleep 2000
                //let rtnUserId2 = apiUsersIfUserExists (hostUser, userName)
                Assert.GreaterOrEqual(createdUserId.AsInteger(), 1)
            else
                Assert.GreaterOrEqual(rtnUserId, 1)
        else
            Assert.AreEqual(roleName.ToString(), "HOSTUSER")


let DataLoadUsers userNamePrefix roleName quantity =
    context "WebAPI Data Loading - Users (Bing)"
    (sprintf "WebAPI Prepare for Users in role %A" roleName) @@@ fun _ -> 
                
        let hostUser = apiLogin config.Site.HostUserName config.Site.DefaultPassword

        //System.Threading.Thread.Sleep 1000
        for i in 1..quantity do
            let userName = userNamePrefix + i.ToString()
            let newUserInfo : APICreateUserInfo = 
                    { 
                        FirstName = userName
                        LastName = "DnnTester"
                        UserName = userName
                        Password = config.Site.DefaultPassword
                        EmailAddress = userName + "DnnTester@mailinator.com" 
                        DisplayName = userName + roleName.ToString()
                        UserID = "0"
                    }
            let createdUser = apiCreateUser (hostUser, newUserInfo, true) 
            let sampleUserCreated = JsonValue.Parse(createdUser.EntityBody.Value)
            if createdUser.StatusCode = 200 then
                let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
                if roleName <> APIRoleName.REGISTEREDUSER then
                    apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName    
                //System.Threading.Thread.Sleep 100

let DataLoadBulkPages = 
    context "WebAPI Data Loading - Pages for Performance Testing (Bing)"
    (sprintf "WebAPI Prepare for pages" ) @@@ fun _ -> 
        let defaultHostLoginInfo = BVTLogInDataPreparation  APIRoleName.HOSTUSER 
        let response = apiSaveBulkPages(defaultHostLoginInfo, "", true, true)
        let rtnPages = JsonValue.Parse(response.EntityBody.Value).GetProperty("Response").GetProperty("pages").AsArray()
        printfn "return pages: %A" rtnPages


let DataLoadBulkPortals portalNamePrefix quantity = 
        let defaultHostLoginInfo = BVTLogInDataPreparation  APIRoleName.HOSTUSER 
        let response = apiCreateBulkPortals(defaultHostLoginInfo, portalNamePrefix, quantity, true)
        printfn "return pages: %A" response

let BVTPagesCreation roleName =
    context "WebAPI BVT (Bing)"
    (sprintf "WebAPI Prepare pages  %A" roleName) @@@ fun _ -> 
    //let loginInfo = BVTLogInDataPreparation roleName
    Assert.AreEqual(1, 1)
     
// mode=0 means random GUID
let loadCreateUsers prefixName quantity mode  = 
    (true)
//    let myLoginUserInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER 
//    apiCreateUsersBatch  (myLoginUserInfo, prefixName, quantity, true) |> ignore
//    let mutable newUserInfo : APICreateUserInfo = 
//        { 
//            FirstName = "FirstNameReplaceMe"
//            LastName = "LastName"
//            UserName = "UserNameReplaceMe"
//            Password = config.Site.DefaultPassword
//            EmailAddress = "EmailAddressReplaceMe" 
//            DisplayName = "DisplayNameReplaceMe"
//        }
//        
//        
//    for i in 1..quantity do
//        let myFirstName = prefixName + 
//                                        match mode with 
//                                            | 0 ->  System.Guid.NewGuid().ToString()   // mode=0 means random GUID
//                                            | _ -> i.ToString()
//        
//            
//        newUserInfo.FirstName <- myFirstName
//        newUserInfo.UserName <- myFirstName
//        newUserInfo.EmailAddress <- myFirstName + "@mailinator.com"
//        newUserInfo.DisplayName <- myFirstName
//        apiCreateUser (myLoginUserInfo, newUserInfo, true) |> ignore
//        printfn "  TC Executed by User Loading: %A" myLoginUserInfo.UserName

let DataLoadForEachPortal portalNamePrefix quantity = 
    context "WebAPI Data Loading - Users (Bing)"
    (sprintf "WebAPI Prepare for Users in Portal") @@@ fun _ ->       
        // Main Portal
//        setAPIURL ""       
        let roleList =
            match testedProduct with
                | Content | ContentBasic ->
                        [
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.CONTENTMANAGERS
                        APIRoleName.CONTENTEDITORS
                        APIRoleName.REGISTEREDUSER
                        ] 
                | Engage ->
                        [
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.CONTENTMANAGERS
                        APIRoleName.CONTENTEDITORS
                        APIRoleName.MODERATORS
                        APIRoleName.COMMUNITYMANAGER
                        APIRoleName.REGISTEREDUSER
                        ] 
                | _ -> 
                        [
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.REGISTEREDUSER
                        ] 
//        roleList |> List.iter BVTLogInAccountsPreSet
        //Create 2 child portal: securitychild1 and securitychild2
        let defaultHostLoginInfo = BVTLogInDataPreparation  APIRoleName.HOSTUSER 
        DataLoadBulkPortals "secChild" 2 |> ignore
        setAPIURL "secChild1"
        roleList |> List.iter BVTLogInAccountsPreSetFun
        setAPIURL "secChild2"
        roleList |> List.iter BVTLogInAccountsPreSetFun

        setAPIURL ""
        arrayAdmin.[0] <- BVTLogInDataPreparation  APIRoleName.ADMINISTRATORS 
        setAPIURL "secChild1"
        arrayAdmin.[1] <- BVTLogInDataPreparation  APIRoleName.ADMINISTRATORS 
        setAPIURL "secChild2"
        arrayAdmin.[2] <- BVTLogInDataPreparation  APIRoleName.ADMINISTRATORS 

        //Console.WriteLine ("arrayAdmin: "+arrayAdmin.[2].ToString())
        
let SecAdminLogsItemsGet testRole = 
    context "WebAPI BVT (Bing)"
    "Security - WebAPI AdminLogs Get Log Items-"+testRole.ToString() @@@ fun _ ->  
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        setAPIURL ""
        let response = getAdminLogsAPIs (arrayAdmin.[0], "GetLogItems?portalId=-2&logType=*&pageSize=10&pageIndex=1", true)
        Assert.AreEqual(response.StatusCode, 200)

        setAPIURL "secChild1"
        let response = getAdminLogsAPIs (arrayAdmin.[1], "GetLogItems?portalId=-2&logType=*&pageSize=10&pageIndex=1", true)
        Assert.AreEqual(response.StatusCode, 200)

        setAPIURL "secChild2"
        let response = getAdminLogsAPIs (arrayAdmin.[2], "GetLogItems?portalId=-2&logType=*&pageSize=10&pageIndex=1", true)
        Assert.AreEqual(response.StatusCode, 200)

        Console.WriteLine "Child1 Admin against Main"
        setAPIURL ""
        let response = getAdminLogsAPIs (arrayAdmin.[1], "GetLogItems?portalId=-2&logType=*&pageSize=10&pageIndex=1", true)
        Assert.AreEqual(response.StatusCode, 302)
        
        Console.WriteLine "Main Admin against Child1"
        setAPIURL "secChild1"
        let response = getAdminLogsAPIs (arrayAdmin.[0], "GetLogItems?portalId=-2&logType=*&pageSize=10&pageIndex=1", true)
        Assert.AreEqual(response.StatusCode, 302)



        // Generate 10K users
        //DataLoadUsers "BingTester" APIRoleName.REGISTEREDUSER 100
        
        // Generate a bulk pages on main portal
        //DataLoadBulkPages


    
let all _ = 
    
    if 1 = 0 then
        let roleList =
            match testedProduct with
                | Content | ContentBasic ->
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.CONTENTMANAGERS
                        APIRoleName.CONTENTEDITORS
                        APIRoleName.REGISTEREDUSER
                        ] 
                | Engage ->
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.CONTENTMANAGERS
                        APIRoleName.CONTENTEDITORS
                        APIRoleName.MODERATORS
                        APIRoleName.COMMUNITYMANAGER
                        APIRoleName.REGISTEREDUSER
                        ] 
                | _ -> 
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.REGISTEREDUSER
                        ] 
    
        let testCaseList = [
                            
                           ]
        roleList |> List.iter BVTLogInAccountsPreSet
        //roleList |> List.iter (fun role -> testCaseList |> List.iter (fun testcase -> testcase role))
    
    else 
        let roleList =
            match testedProduct with
                | Content | ContentBasic ->
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.CONTENTMANAGERS
                        APIRoleName.CONTENTEDITORS
                        APIRoleName.REGISTEREDUSER
                        ] 
                | Engage ->
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.CONTENTMANAGERS
                        APIRoleName.CONTENTEDITORS
                        APIRoleName.MODERATORS
                        APIRoleName.COMMUNITYMANAGER
                        APIRoleName.REGISTEREDUSER
                        ] 
                | _ -> 
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.REGISTEREDUSER
                        ] 
        roleList |> List.iter BVTLogInAccountsPreSet
        DataLoadForEachPortal "secChild" 2
        SecAdminLogsItemsGet APIRoleName.ADMINISTRATORS
 





