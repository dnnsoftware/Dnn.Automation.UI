module DataPreparation


open canopy
open HttpFs.Client
open DnnCanopyContext
open DnnTypes
open DnnUserLogin
open APIHelpers
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
                | APIRoleName.ADMINISTRATORS -> userNamePrefix+"BingADMIN"            
                | APIRoleName.CONTENTMANAGERS -> userNamePrefix+"BingCM"
                | APIRoleName.CONTENTEDITORS -> userNamePrefix+"BingCE"
                | APIRoleName.MODERATORS -> userNamePrefix+"BingMOD"
                | APIRoleName.COMMUNITYMANAGER -> userNamePrefix+"BingCOM"
                | _ -> userNamePrefix+"BingTesterRU"
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
                        Authorize = "true"
                    }
            let createdUser = apiCreateUser (hostUser, newUserInfo, true) 
            let sampleUserCreated = JsonValue.Parse(createdUser.EntityBody.Value)
            if createdUser.statusCode = 200 then
                let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
                if roleName <> APIRoleName.REGISTEREDUSERS then
                    apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName    
                //System.Threading.Thread.Sleep 100

let DataUnAuthorizedUser userNamePrefix roleName quantity =
    context "WebAPI Data Loading - Users (Bing)"
    (sprintf "WebAPI Prepare for UnAuthorized User in role %A" roleName) @@@ fun _ -> 
                
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
                        Authorize = "false"
                    }
            let createdUser = apiCreateUser (hostUser, newUserInfo, true) 
            let sampleUserCreated = JsonValue.Parse(createdUser.EntityBody.Value)
            if createdUser.StatusCode = 200 then
                let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
                if roleName <> APIRoleName.REGISTEREDUSERS then
                    apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName    
                //System.Threading.Thread.Sleep 100

let DataSoftDeleteUser userNamePrefix roleName quantity = 
    context "WebAPI BVT (Bing)"
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
                        Authorize = "true"
                    }
            let createdUser = apiCreateUser (hostUser, newUserInfo, true) 
            let sampleUserCreated = JsonValue.Parse(createdUser.EntityBody.Value)
            if createdUser.StatusCode = 200 then
                let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
                if roleName <> APIRoleName.REGISTEREDUSERS then
                    apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName
                let response = apiSoftDeleteUser (hostUser, createdUserId, true)
                printfn "  Soft Delete New User %A" newUserInfo.UserName
                //System.Threading.Thread.Sleep 100

let DataUnAuthorizedAndSoftDeletedUser userNamePrefix roleName quantity = 
    context "WebAPI BVT (Bing)"
    (sprintf "WebAPI Prepare for Users in Role, Unauthorized And Soft Deleted %A" roleName) @@@ fun _ -> 
        
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
                        Authorize = "false"
                    }
            let createdUser = apiCreateUser (hostUser, newUserInfo, true) 
            let sampleUserCreated = JsonValue.Parse(createdUser.EntityBody.Value)
            if createdUser.StatusCode = 200 then
                let createdUserId = sampleUserCreated.GetProperty("userId").AsString()
                if roleName <> APIRoleName.REGISTEREDUSERS then
                    apiRolesAddUserToRole (hostUser, createdUserId, roleName, true) |> ignore
                printfn "  Created New User %A" newUserInfo.UserName
                let response = apiSoftDeleteUser (hostUser, createdUserId, true)
                printfn "  Soft Delete New User %A" newUserInfo.UserName
                //System.Threading.Thread.Sleep 100        


let DataLoadBulkPages pageNm = 
    context "WebAPI Data Loading - Pages for Performance Testing (Bing)"
    (sprintf "WebAPI Prepare for pages" ) @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation  APIRoleName.HOSTUSER 
        //(loginInfo:UserLoginInfo, bulkString:string, withPublish:bool, visible:bool, softDeleted:bool, withLog:bool)
        let response = apiSaveBulkPages(myLoginUserInfo, "", 1, "", "", "", true, true, false, true)
        let rtnPages = JsonValue.Parse(response.EntityBody.Value).GetProperty("Response").GetProperty("pages").AsArray()
        printfn "return pages: %A" rtnPages
        System.Threading.Thread.Sleep 30000

let DataLoadBulkPagesDeeper pageName workflow trackLinks startDate endDate publish visible deleted= 
    context "WebAPI Data Loading - Pages for Export Import (Bing)"
    (sprintf "WebAPI Prepare for pages with more paramenters" ) @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation  APIRoleName.HOSTUSER 
        let mutable myPostString = """{"bulkPages":"GoDeep\n>GoDeep-1\n>>GoDeep-1-1\n>>>GoDeep-1-1-1\n>>>>GoDeep-1-1-1-1","parentId":"-1","keywords":"","tags":"","includeInMenu":true,"startDate":null,"endDate":null}"""
        myPostString <- myPostString.Replace ("GoDeep", pageName)
        //(loginInfo:UserLoginInfo, bulkString:string, withPublish:bool, visible:bool, softDeleted:bool, withLog:bool)
        let response = apiSaveBulkPages(myLoginUserInfo, myPostString, workflow, trackLinks, startDate, endDate, publish, visible, deleted, true)
        let rtnPages = JsonValue.Parse(response.EntityBody.Value).GetProperty("Response").GetProperty("pages").AsArray()
        printfn "return pages: %A" rtnPages
        System.Threading.Thread.Sleep 30000

       

let DataLoadBulkPortals portalNamePrefix quantity = 
    context "WebAPI Data Loading - Lots of portals (Bing)"
    (sprintf "WebAPI Prepare for portals" ) @@@ fun _ -> 
        let defaultHostLoginInfo = BVTLogInDataPreparation  APIRoleName.HOSTUSER 
        let response = apiCreateBulkPortals(defaultHostLoginInfo, portalNamePrefix, quantity, true)
        printfn "return pages: %A" response

let DataLoadPortal portalName  = 
    context "WebAPI Data Loading - Lots of portals (Bing)"
    (sprintf "WebAPI Prepare for portals" ) @@@ fun _ -> 
        APIURL <- config.Site.SiteAlias
        let defaultHostLoginInfo = BVTLogInDataPreparation  APIRoleName.HOSTUSER 
        let response = apiCreatePortal(defaultHostLoginInfo, portalName, true)
        APIURL <- config.Site.SiteAlias+"/"+JsonValue.Parse(response.EntityBody.Value).GetProperty("Portal").GetProperty("PortalName").AsString()
        printfn "response pages: %A" response

//"AutoRole" "-1" "0" "false" "false" "false" 1
let DataCreateRolesMore roleNamePrefix groupId status isPublic autoAssign isSystem quantity = 
    context "WebAPI Load Data for roles (Bing)"
    (sprintf "WebAPI Roles Create Role by more info" )   @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER 
        
        //System.Threading.Thread.Sleep 1000
        for i in 1..quantity do
            let newRoleInfo : APICreateRoleInfo = 
                        { 
                            Id = "-1"
                            Name = roleNamePrefix + i.ToString()
                            GroupId = groupId
                            Description = ""
                            SecurityMode = "0"
                            Status = status
                            IsPublic = isPublic
                            AutoAssign = autoAssign
                            IsSystem = isSystem
                        }

            let response = apiRolesSaveRoleMore(myLoginUserInfo, newRoleInfo, true)   // 0 means get a random number. 
            if response.StatusCode = 200 then
                let newId = JsonValue.Parse(response.EntityBody.Value).GetProperty("id").AsString()
                printfn "  Role created : %A" newId
            else
                printfn "  Role Creation FAILed"
              //Validation by user role
           
let DataCreatePageMore   = 
    context "WebAPI BVT Data Load (Bing)"
    (sprintf "WebAPI Create Pages with more info")  @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER 
        //let defaultHostLoginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER 
        let portalID = "0"
        let response = apiPagesSavePageDetails(myLoginUserInfo, portalID, "", true)
        printfn "  TC Executed by User: %A" myLoginUserInfo.UserName
          //Validation by user role
       
let VocabularyCreate vocName typeId scopeTypeId termQuantity = 
    context "WebAPI BVT data loading (Bing)"
    (sprintf "WebAPI Create Vocabulary and Terms")  @@@ fun _ -> 

        let myLoginUserInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER 
        
        let mutable postString = SamplePostVocabulary
        let myRandomStr = System.Guid.NewGuid().ToString()
        let myVocName = 
            match vocName with
                | "" -> "Voc-"+myRandomStr
                | _ -> vocName
        postString <- postString.Replace ("vocabularyNameReplaceMe", myVocName)
        postString <- postString.Replace ("DescriptionReplaceMe", "Description Of " + myVocName)
        postString <- postString.Replace ("ScopeTypeIdReplaceMe", scopeTypeId)
        postString <- postString.Replace ("TypeIdReplaceMe", typeId)
        
        // Create vocabulary
        let response = postVocabularyAPIs(myLoginUserInfo, "CreateVocabulary", postString, true)
        let samples = JsonValue.Parse(response.EntityBody.Value)
        let myVocId = samples.GetProperty("VocabularyId").AsString()
        let mutable myTermId = "-1"
        // Create Terms for that myVocId/myVocName
        for i in 1..termQuantity do
            postString <- SamplePostVocabularyTermCreation
            let myRandomStr = System.Guid.NewGuid().ToString()
            postString <- postString.Replace ("VocabularyIdReplaceMe", myVocId)
            postString <- postString.Replace ("TermNameReplaceMe", "TermName-" + i.ToString())
            postString <- postString.Replace ("TermDescriptionReplaceMe", "Description-" + myRandomStr)
            postString <- postString.Replace ("ParentTermIdReplaceMe", myTermId)
            let termResponse = postVocabularyAPIs(myLoginUserInfo, "CreateTerm", postString, false) 
            if typeId = "2" then //Hierachy
                let samples = JsonValue.Parse(termResponse.EntityBody.Value)
                myTermId <- samples.GetProperty("TermId").AsString()
            else
                myTermId <- "-1"
            
let AddTextContentsToPage pageName path = 
    context "WebAPI BVT Data Load (Bing)"
    (sprintf "WebAPI Create Pages with more info")  @@@ fun _ -> 
        let myLoginUserInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER 
        //let defaultHostLoginInfo = BVTLogInDataPreparation APIRoleName.HOSTUSER 
        let portalID = "0"
        let myPagePathFullName = 
            match path with
                | "" -> "/" + pageName
                | _ -> path + pageName
        let response = apiPagesGetPageList(myLoginUserInfo, portalID, pageName, false)
        if response.StatusCode = 200 then
            let rtnPages = JsonValue.Parse(response.EntityBody.Value).AsArray()
            let tabIDArray = rtnPages |> Array.find (fun oneTabOnly -> oneTabOnly.GetProperty("tabpath").AsString() = myPagePathFullName)
            let myTabId = tabIDArray.GetProperty("id").AsString()
            printfn "  Found TabID: %A" myTabId
            
 
    
let all _ = 
    
    if 1 = 0 then
        // Generate users with default Roles
        //DataLoadUsers "BingTester" APIRoleName.REGISTEREDUSERS 100
        //DataLoadUsers "BingIETrl" APIRoleName.TRANSLATORUS 10
        DataLoadUsers "BingIERU" APIRoleName.REGISTEREDUSERS 10
        DataLoadUsers "BingIEAdm" APIRoleName.ADMINISTRATORS 10
        DataLoadUsers "BingIESub" APIRoleName.SUBSCRIBERS 10
        DataLoadUsers "BingIECM" APIRoleName.CONTENTMANAGERS 10
        DataLoadUsers "BingIECE" APIRoleName.CONTENTEDITORS 10
        DataLoadUsers "BingIECOM" APIRoleName.COMMUNITYMANAGER 10
        //Soft Deleted User
        DataSoftDeleteUser "BingIERUDeleted" APIRoleName.REGISTEREDUSERS 10
        // Unauthorized
        DataUnAuthorizedUser "BingIERUUnAuth" APIRoleName.REGISTEREDUSERS 10
        // Unauthorized + Soft Deleted
        DataUnAuthorizedAndSoftDeletedUser "BingIERUDelUnAuth" APIRoleName.REGISTEREDUSERS 10
        // Generate a bulk pages on main portal
        //DataLoadBulkPages
        
        // Pages with deeper layer, no publish. 
        //DataLoadBulkPagesDeeper pageName workflow trackLinks startDate endDate publish visible deleted
        DataLoadBulkPagesDeeper "GoDeeper" 3 "" "" "" true true false
        
        // All disabled
        DataCreateRolesMore "RoleAllFalse" "-1" "0" "false" "false" "false" 1
        DataCreateRolesMore "RolePublic" "-1" "0" "true" "false" "false" 1
        DataCreateRolesMore "RolePubAndAutoAssign" "-1" "0" "true" "true" "false" 1
        DataCreateRolesMore "RoleSystem" "-1" "0" "false" "false" "true" 1
        DataCreateRolesMore "RoleAutoAndSystem" "-1" "0" "false" "true" "true" 1
        DataCreateRolesMore "RoleAllTrue" "-1" "0" "true" "true" "true" 1
        DataCreateRolesMore "RoleAutoAssign" "-1" "0" "false" "false" "true" 1

        // Enabled
        DataCreateRolesMore "ENBRoleAllFalse" "-1" "1" "false" "false" "false" 1
        DataCreateRolesMore "ENBRolePublic" "-1" "1" "true" "false" "false" 1
        DataCreateRolesMore "ENBRolePubAndAutoAssign" "-1" "1" "true" "true" "false" 1
        DataCreateRolesMore "ENBRoleSystem" "-1" "1" "false" "false" "true" 1
        DataCreateRolesMore "ENBRoleAutoAndSystem" "-1" "1" "false" "true" "true" 1
        DataCreateRolesMore "ENBRoleAllTrue" "-1" "1" "true" "true" "true" 1
        DataCreateRolesMore "ENBRoleAutoAssign" "-1" "1" "false" "false" "true" 1

        // Pending
        DataCreateRolesMore "PndRoleAllFalse" "-1" "-1" "false" "false" "false" 1
        DataCreateRolesMore "PndRolePublic" "-1" "-1" "true" "false" "false" 1
        DataCreateRolesMore "PndRolePubAndAutoAssign" "-1" "-1" "true" "true" "false" 1
        DataCreateRolesMore "PndRoleSystem" "-1" "-1" "false" "false" "true" 1
        DataCreateRolesMore "PndRoleAutoAndSystem" "-1" "-1" "false" "true" "true" 1
        DataCreateRolesMore "PndRoleAllTrue" "-1" "-1" "true" "true" "true" 1
        DataCreateRolesMore "PndRoleAutoAssign" "-1" "-1" "false" "false" "true" 1
    else 
        let roleList = [
                            APIRoleName.HOSTUSER 
                       ]
        roleList |> List.iter BVTLogInAccountsPreSet

        DataLoadBulkPortals "portala" 2

//        
//
//
//        DataLoadUsers "BingIERU" APIRoleName.REGISTEREDUSERS 10
//        DataLoadUsers "BingIEAdm" APIRoleName.ADMINISTRATORS 10
//        DataLoadUsers "BingIESub" APIRoleName.SUBSCRIBERS 10
//        DataLoadUsers "BingIECM" APIRoleName.CONTENTMANAGERS 10
//        DataLoadUsers "BingIECE" APIRoleName.CONTENTEDITORS 10
//        DataLoadUsers "BingIECOM" APIRoleName.COMMUNITYMANAGER 10
//        //Soft Deleted User
//        DataSoftDeleteUser "BingIERUDeleted" APIRoleName.REGISTEREDUSERS 10
//        // Unauthorized
//        DataUnAuthorizedUser "BingIERUUnAuth" APIRoleName.REGISTEREDUSERS 10
//        // Unauthorized + Soft Deleted
//        DataUnAuthorizedAndSoftDeletedUser "BingIERUDelUnAuth" APIRoleName.REGISTEREDUSERS 10
//        // Generate a bulk pages on main portal
//        //DataLoadBulkPages
//        
//        // Pages with deeper layer, no publish. 
//        //DataLoadBulkPagesDeeper pageName workflow trackLinks startDate endDate publish visible deleted
//        DataLoadBulkPagesDeeper "GoDeeper" 3 "" "" "" true true false
//        
//        // All disabled
//        DataCreateRolesMore "RoleAllFalse" "-1" "0" "false" "false" "false" 1
//        DataCreateRolesMore "RolePublic" "-1" "0" "true" "false" "false" 1
//        DataCreateRolesMore "RolePubAndAutoAssign" "-1" "0" "true" "true" "false" 1
//        DataCreateRolesMore "RoleSystem" "-1" "0" "false" "false" "true" 1
//        DataCreateRolesMore "RoleAutoAndSystem" "-1" "0" "false" "true" "true" 1
//        DataCreateRolesMore "RoleAllTrue" "-1" "0" "true" "true" "true" 1
//        DataCreateRolesMore "RoleAutoAssign" "-1" "0" "false" "false" "true" 1
//
//        // Enabled
//        DataCreateRolesMore "ENBRoleAllFalse" "-1" "1" "false" "false" "false" 1
//        DataCreateRolesMore "ENBRolePublic" "-1" "1" "true" "false" "false" 1
//        DataCreateRolesMore "ENBRolePubAndAutoAssign" "-1" "1" "true" "true" "false" 1
//        DataCreateRolesMore "ENBRoleSystem" "-1" "1" "false" "false" "true" 1
//        DataCreateRolesMore "ENBRoleAutoAndSystem" "-1" "1" "false" "true" "true" 1
//        DataCreateRolesMore "ENBRoleAllTrue" "-1" "1" "true" "true" "true" 1
//        DataCreateRolesMore "ENBRoleAutoAssign" "-1" "1" "false" "false" "true" 1
//
//        // Pending
//        DataCreateRolesMore "PndRoleAllFalse" "-1" "-1" "false" "false" "false" 1
//        DataCreateRolesMore "PndRolePublic" "-1" "-1" "true" "false" "false" 1
//        DataCreateRolesMore "PndRolePubAndAutoAssign" "-1" "-1" "true" "true" "false" 1
//        DataCreateRolesMore "PndRoleSystem" "-1" "-1" "false" "false" "true" 1
//        DataCreateRolesMore "PndRoleAutoAndSystem" "-1" "-1" "false" "true" "true" 1
//        DataCreateRolesMore "PndRoleAllTrue" "-1" "-1" "true" "true" "true" 1
//        DataCreateRolesMore "PndRoleAutoAssign" "-1" "-1" "false" "false" "true" 1
//
//        //DataLoadBulkPagesDeeper pageName workflow trackLinks startDate endDate publish visible deleted
//        DataLoadBulkPagesDeeper "pubANDdel" 3 "" "" ""  true true true
//        DataLoadBulkPagesDeeper "pubNotVis" 3 "" "" ""  true false false
//        DataLoadBulkPagesDeeper "NoPubOrVis" 3 "" "" "" false false false
//
//        let endDate = "\"2017-04-30T23:59:59\""
//        let startDate = "\"2017-04-15T00:00:00\""
//        DataLoadBulkPagesDeeper "NoPubWithDate" 3 "" startDate endDate false false false
//        DataLoadBulkPagesDeeper "NoPubWithDateAndTrack" 3 "true" startDate endDate false false false
//        DataLoadBulkPagesDeeper "pubWithDateAndTrack" 3 "true" startDate endDate true true false
//
//        // Generate vocabulary and Terms
//        VocabularyCreate "SimpleAppVoc1" "1" "1" 30
//        VocabularyCreate "SimpleWebVoc1" "1" "2" 30
//        VocabularyCreate "HierarchyAppVoc1" "2" "1" 20
//        VocabularyCreate "HierarchyWebVoc1" "2" "2" 20

        // Add text contents to a page
        //AddTextContentsToPage "GoDeeper" "/"


        

