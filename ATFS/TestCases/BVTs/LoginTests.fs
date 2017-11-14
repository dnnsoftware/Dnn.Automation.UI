module LoginTests

open canopy
open DnnCanopyContext
open DnnUserLogin

//page info
let private loginPage = "/Login"

//tests
let positive _ = 
    context "positive login page tests"
    "Host login through popup and check successful login" @@@ fun _ -> 
        logOff()
        loginAsHost()
        //displayed "#ControlNav"
        siteSettings.loggedinUserNameLinkId == hostDisplayName
        displayed siteSettings.loggedinUserImageLinkId
    "Host login through page and check welcome text" @@@ fun _ -> 
        logOff()
        loginOnPageAs Host
        //displayed "#ControlNav"
        displayed siteSettings.loggedinUserImageLinkId
        siteSettings.loggedinUserNameLinkId == hostDisplayName

//more tests
let negative _ = 
    context "negative login page tests"
    "Wrong password displays login error" @@@ fun _ -> 
        logOff()
        let badCreds = RegisteredUser(hostUsername, "wrong.password")
        loginPopupAs badCreds
        if not (existsAndVisible SkinMsgErrorSelector) then failwith "The host login attempt must have failed!"

let all _ = 
    positive()
    negative()
