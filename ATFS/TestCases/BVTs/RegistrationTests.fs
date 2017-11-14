module RegistrationTests

open canopy
open DnnCanopyContext
open DnnUserLogin
open DnnAddUser
open DnnAddToRole

let mutable private userInfo =
    { UserName = ""
      DisplayName = ""
      EmailAddress = ""
      Password = ""
      ConfirmPass = "" }

//tests
let positive1 _ = 

    // perform something after clicking register new user (validates some items)
    let afterClickRegister() = 
        // Assert for success
        let skinMessageId = "//div[contains(@id,'_dnnSkinMessage')]"
        if existsAndVisible skinMessageId then 
            let e = element skinMessageId
            e == emailDetailsSentText
            e != userNameExistsText    

    context "User Registration Tests"

    "Create new user" @@@ fun _ -> 
        userInfo <- createRandomUser()
        registerUser userInfo ignore

    "Approve the new user as Host and Login as User" @@@ fun _ ->
        if not (existsAndVisible siteSettings.loggedinUserImageLinkId) then 
            approveUser userInfo.UserName Host
            loginAsRegisteredUser userInfo.UserName userInfo.Password
            CheckSkinValidationError "Newly registered user is not authorized to login!"
            siteSettings.loggedinUserNameLinkId == userInfo.DisplayName
            //displayed siteSettings.loggedinUserImageLinkId

    "Add user to Admins role" @@@ fun _ -> 
        loginOnPageAs Host
        addUserToRole userInfo.UserName adminsRoleName

let all _ =
    positive1()
