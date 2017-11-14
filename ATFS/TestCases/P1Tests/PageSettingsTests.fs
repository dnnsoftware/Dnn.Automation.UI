module PageSettingsTests

open DnnCanopyContext
open DnnUserLogin
open DnnPageSettings

let private testPageName = homePageName
let private testPageUrl = "/"

//tests
let positive _ = 
    //============================================================
    context "Test Page Settings"

    "Page Settings | Modify Page Details" @@@ fun _ -> 
        loginAsHost()
        let settings = 
            { Name = testPageName
              Title = testPageName + " Title"
              RelativeUrl = testPageUrl
              DoNotRedirect = NOCHANGE
              Description = ""
              Keywords = "New Keyword 1,New Keyword2"
              ParentPage = ""
              IncludeInMenu = TRUE }
        modifyPageDetails testPageUrl settings
        //saveSettings()

    //Test case disabled since modifyPermissions not implemented yet
    //"Page Settings | Modify Permissions" @@@ fun _ -> 
    //    loginAsHost()
    //    let settings = 
    //        { AllUsersViewPage = GRANT
    //          AllUsersEditPage = CLEAR
    //          RegisteredUsersViewPage = DONTCHANGE
    //          RegisteredUsersEditPage = DONTCHANGE }
    //    modifyPermissions testPageUrl settings
    //    //saveSettings()

(*
    "Page Settings | Modify Advanced Settings" @@@ fun _ ->
        openPageSettings testPageUrl
        modifyAdvancedSettings testPageUrl settings
        saveSettings()

    "Page Settings | After All Tests" @@@ fun _ ->
        logOff()
    *)

let all _ = positive()
