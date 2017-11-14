module BVTBing


open canopy
open HttpFs.Client
open DnnCanopyContext
open DnnManager
open DnnTypes
open DnnWebApi
open DnnSiteCreate
open DnnUserLogin
open APIHelpers
open BVTBingHelper
open NUnit.Framework
open FSharp.Data



//page info
let private loginPage = "/Login"
type RoleTypeToTest = 
    | HOSTUSER
    | ADMINISTRATORS
    | CONTENTMANAGERS
    | CONTENTEDITORS
type UserRole = {LogInUserRole : string}


let mutable myLoginInfo : UserLoginInfo =     { UserName = ""
                                                Password = ""
                                                DisplayName = ""
                                                DNNCookie = { name=""; value="" }
                                                RVCookie = { name=""; value="" }
                                                RVToken = { name=""; value="" }
                                                }


let mutable myHostLoginInfo : UserLoginInfo =  {UserName = ""
                                                Password = ""
                                                DisplayName = ""
                                                DNNCookie = { name=""; value="" }
                                                RVCookie = { name=""; value="" }
                                                RVToken = { name=""; value="" }
                                                }

let BVTLogInDataPreparation (roleName:APIRoleName) =

    canopy.configuration.skipRemainingTestsInContextOnFailure <- true
    let userNamePrefix = if useChildPortal then "C" else ""
    match roleName  with
        | APIRoleName.ADMINISTRATORS -> 
                if myLoginInfo.UserName <> userNamePrefix+"AutoADMIN" then myLoginInfo <- apiLoginAsAdmin()
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

let BVTLogInAccountsPreSet roleName =
    context "WebAPI Data Loading - PreSet Accounts (Bing)"
    (sprintf "WebAPI Prepare for Users in role %A" roleName) @@@ fun _ -> 
        
        
        let hostUser = apiLogin config.Site.HostUserName config.Site.DefaultPassword
        let userNamePrefix = if useChildPortal then "C" else ""
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
                      Authorize = "true"
                    }
                System.Threading.Thread.Sleep 1000
                let createdUser = apiCreateUser (hostUser, newUserInfo, true) 
                let sampleUserCreated = JsonValue.Parse(createdUser.EntityBody.Value)
                let createdUserId = sampleUserCreated.GetProperty("Results").GetProperty("userId").AsString()
                apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName    
                System.Threading.Thread.Sleep 2000
                //let rtnUserId2 = apiUsersIfUserExists (hostUser, userName)
                Assert.GreaterOrEqual(createdUserId.AsInteger(), 1)
            else
                Assert.GreaterOrEqual(rtnUserId, 1)
        else
            Assert.AreEqual(roleName.ToString(), "HOSTUSER")


let PBManagerVisibility testRole =
    context "UI PB > Manage Visibility (Bing)"
    (sprintf "UI PB > Manage Visibility > %A" testRole) @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        waitPageLoad()
        let response = existsAndVisible BVTBingHelper.ID_PB_Manage
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB > Manage > Users Visibility > %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Manage then hoverOver BVTBingHelper.ID_PB_Manage
        let response = existsAndVisible BVTBingHelper.ID_PB_Manage_Users
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB > Manage > Roles Visibility - %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Manage then hoverOver BVTBingHelper.ID_PB_Manage
        let response = existsAndVisible BVTBingHelper.ID_PB_Manage_Roles
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB > Manage > Templates Visibility - %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Manage then hoverOver BVTBingHelper.ID_PB_Manage
        let response = existsAndVisible BVTBingHelper.ID_PB_Manage_Templates
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)
            

    (sprintf "UI PB > Manage > AdminLogs Visibility - %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Manage then hoverOver BVTBingHelper.ID_PB_Manage
        let response = existsAndVisible BVTBingHelper.ID_PB_Manage_AdminLogs
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB > Manage > File Management Visibility - %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Manage then hoverOver BVTBingHelper.ID_PB_Manage
        let response = existsAndVisible BVTBingHelper.ID_PB_Manage_FileManagement
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB > Manage > Sites Visibility - %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Manage then hoverOver BVTBingHelper.ID_PB_Manage
        let response = existsAndVisible BVTBingHelper.ID_PB_Manage_Sites
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB > Manage > Themes Visibility - %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Manage then hoverOver BVTBingHelper.ID_PB_Manage
        let response = existsAndVisible BVTBingHelper.ID_PB_Manage_Themes
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS  ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)
 
let PBContentVisibility testRole =
    context "UI PB Content Visibility (Bing)"
    (sprintf "UI PB Content Visibility %A" testRole) @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        waitPageLoad()
        let response = existsAndVisible BVTBingHelper.ID_PB_Content
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS | APIRoleName.CONTENTEDITORS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)
        

    (sprintf "UI PB Content Assets > %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Content then hoverOver BVTBingHelper.ID_PB_Content
        let response = existsAndVisible BVTBingHelper.ID_PB_Content_Assets
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS | APIRoleName.CONTENTEDITORS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB Content Pages > %A" testRole) @@@ fun _ -> 
        if existsAndVisible BVTBingHelper.ID_PB_Content then hoverOver BVTBingHelper.ID_PB_Content
        let response = existsAndVisible BVTBingHelper.ID_PB_Content_Pages
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB Content RecycleBin > %A" testRole) @@@ fun _ -> 
        if existsAndVisible BVTBingHelper.ID_PB_Content then hoverOver BVTBingHelper.ID_PB_Content
        let response = existsAndVisible BVTBingHelper.ID_PB_Content_Recyclebin
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)

    (sprintf "UI PB Content ContentItems > %A" testRole) @@@ fun _ -> 
        if existsAndVisible BVTBingHelper.ID_PB_Content then hoverOver BVTBingHelper.ID_PB_Content
        let response = existsAndVisible BVTBingHelper.ID_PB_Content_ContentItems
        if config.Site.ProductType.ToLower() = "engage" then
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                    Assert.AreEqual(response, true)
                | _ -> 
                    Assert.AreEqual(response, false)
        else
            Assert.AreEqual(response, false)      

    (sprintf "UI PB Content Forms > %A" testRole) @@@ fun _ -> 
        if existsAndVisible BVTBingHelper.ID_PB_Content then hoverOver BVTBingHelper.ID_PB_Content
        let response = existsAndVisible BVTBingHelper.ID_PB_Content_Forms
        if config.Site.ProductType.ToLower() = "engage" then
            match testRole with
                | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                    Assert.AreEqual(response, true)
                | _ -> 
                    Assert.AreEqual(response, false)
        else
            Assert.AreEqual(response, false)                                     


let PBSettingsVisibility testRole =
    context "UI PB Settings Visibility (Bing)"
    (sprintf "UI PB Settings Visibility %A" testRole) @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation testRole
        waitPageLoad()
        let response = existsAndVisible BVTBingHelper.ID_PB_Settings
        //let response = waitForElementPresentBing ID_PB_Manage
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)
        

    (sprintf "UI PB Settings ConfigManager %A" testRole) @@@ fun _ -> 
        //let myLoginUserInfo = BVTLogInDataPreparation testRole
        //waitPageLoad()
        if existsAndVisible BVTBingHelper.ID_PB_Settings then hoverOver BVTBingHelper.ID_PB_Settings
        let response = existsAndVisible BVTBingHelper.ID_PB_Settings_ConfigConsole
        match testRole with
            | APIRoleName.HOSTUSER | APIRoleName.ADMINISTRATORS | APIRoleName.CONTENTMANAGERS ->
                Assert.AreEqual(response, true)
            | _ -> 
                Assert.AreEqual(response, false)


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
                        APIRoleName.REGISTEREDUSERS
                        ] 
                | Engage ->
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.CONTENTMANAGERS
                        APIRoleName.CONTENTEDITORS
                        APIRoleName.MODERATORS
                        APIRoleName.COMMUNITYMANAGER
                        APIRoleName.REGISTEREDUSERS
                        ] 
                | _ -> 
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.REGISTEREDUSERS
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
                        APIRoleName.REGISTEREDUSERS
                        ] 
                | Engage ->
                        [
                        //APIRoleName.HOSTUSER 
                        //APIRoleName.ADMINISTRATORS
                        APIRoleName.CONTENTMANAGERS
                        APIRoleName.CONTENTEDITORS
                        APIRoleName.MODERATORS
                        APIRoleName.COMMUNITYMANAGER
                        APIRoleName.REGISTEREDUSERS
                        ] 
                | _ -> 
                        [
                        APIRoleName.HOSTUSER 
                        APIRoleName.ADMINISTRATORS
                        APIRoleName.REGISTEREDUSERS
                        ] 
    
        let testCaseList = [
                            //PBManagerVisibility
                            //PBContentVisibility
                            PBSettingsVisibility
                           ]

        roleList |> List.iter BVTLogInAccountsPreSet
        roleList |> List.iter (fun role -> testCaseList |> List.iter (fun testcase -> testcase role))


