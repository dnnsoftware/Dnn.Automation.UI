module Register_User

open canopy
open runner
open CanopyExtensions
open DnnTypes
open DnnConfig
open DnnUserLogin
open DnnAddUser
open DnnAddToRole
open DnnUserLogin


let positive _ =
    context "Tesing user registration"

    "Registering a user in the popup should pass" @@@ fun _ ->
        logOff()
        clickDnnPopupLink "#dnn_dnnUser_enhancedRegisterLink"
        let inputs = element "#dnn_ctr_Register_userForm" |> elementsWithin "input"
        inputs.Item(0) << "Admin1"
        inputs.Item(1) << "dnnhost"
        inputs.Item(2) << "dnnhost"
        inputs.Item(3) << "Admin1"
        inputs.Item(4) << "Admin1@dnn.com"
        click "#dnn_ctr_Register_registerButton"
        waitPageLoad()
    "Login as Host and Authourize the User" @@@ fun _ ->
    context "positive login page tests"
    "Host login through popup and check successful login" @@@ fun _ ->
        loginPopupAs Host |> ignore
        displayed "#ControlNav"
        siteSettings.loggedinUserNameLinkId == hostDisplayName
        displayed siteSettings.loggedinUserImageLinkId
        click "dnn_dnnUser_NotificationLink"
        waitPageLoad()



let all _ =
    positive()
