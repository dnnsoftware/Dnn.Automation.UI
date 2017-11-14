module DnnAddUser

open canopy
open DnnUserLogin

let createNewUser uname dispname password confirmpass = 
    { UserName = uname
      DisplayName = dispname
      EmailAddress = uname.Replace(" ", "") + "@test.com"
      Password = password
      ConfirmPass = confirmpass }

let createRandomUser() = 
    let postfix = getRandomId()
    createNewUser ("TestUser" + postfix) ("Test User" + postfix) defaultPassword defaultPassword

let validateRegistrationSuccess() = 
    // Assert for success
    let skinMessageId = "//div[contains(@id,'_dnnSkinMessage') and (@class='dnnFormMessage dnnFormSuccess')]"
    if existsAndVisible skinMessageId then 
        let e = element skinMessageId
        e =~ @"An e-mail with your details has been sent to the website administrator for verification."

let private validateRegistrationFailure() = 
    // Assert for failure
    let skinMessageId = "//div[contains(@id,'_dnnSkinMessage') and (@class='dnnFormMessage dnnFormSuccess')]"
    if existsAndVisible skinMessageId then 
        let e = element skinMessageId
        e =~ @"A User Already Exists For the Username Specified. Please Register Again Using A Different Username."

let registerUser (userInfo : RegisterUserInfo) afterRegisterCallback = 
    logOff()
    clickDnnPopupLink siteSettings.registerLinkId
    //// this is more flexible but much slower due to recursion
    //textBoxOf "User Name:" << userInfo.UserName
    //textBoxOf "Password:" << userInfo.Password
    //textBoxOf "Confirm Password:" << userInfo.ConfirmPass
    //textBoxOf "Display Name:" << userInfo.DisplayName
    //textBoxOf "Email Address:" << userInfo.EmailAddress
    let inputs = inputsOf "#dnn_ctr_Register_userForm"
    inputs.[0] << userInfo.UserName
    inputs.[1] << userInfo.Password
    inputs.[2] << userInfo.ConfirmPass
    inputs.[3] << userInfo.DisplayName
    inputs.[4] << userInfo.EmailAddress
    click registerNewUserBtn
    waitForAjax()
    sleep 1
    // call user supplied function to check stuff
    afterRegisterCallback()
    validateRegistrationSuccess |> ignore
    closePopup()

let approveUser (username : string) (byUser : DnnUser) = 
    loginPopupAs byUser
    openPBUsers()
    let searchTB = "div.users-filter-container>div>div>div.dnn-search-box>input"
    let userName = sprintf "//p[.='%s']" username
    //switch to unauthorized users
    let ddnIcon = "//div[@class='user-filters-filter']/div/div[@class='dropdown-icon']"
    let unauthLink = sprintf "//div/ul/li[.='%s']" UnauthorizedText
    click ddnIcon
    waitForElementPresent unauthLink
    click unauthLink
    waitForAjax()
    //if username not visible, search for user
    if not(existsAndVisible userName) then
        searchTB << username
        waitForAjax()
        waitForElementPresent userName
    let userEllipsis = userName + "/../../div/div[not(@title)]/div/*"
    let authorizeLink = userEllipsis + "/../../div[contains(@class,'dnn-user-menu')]/ul/li[contains(.,\"" + authorizeUserText + "\")]"
    click userEllipsis
    waitForElementPresent authorizeLink
    click authorizeLink
    waitForAjax()
    closePersonaBar()

let registerUserAsHost (userInfo : RegisterUserInfo) afterRegisterCallback = 
    loginAsHost()
    openPBUsers()
    click (sprintf "//button[.=\"%s\"]" addUserText)
    "//input[@tabindex='1']" << userInfo.DisplayName
    "//input[@tabindex='2']" << "User"
    "//input[@tabindex='3']" << userInfo.UserName
    "//input[@tabindex='4']" << userInfo.EmailAddress
    "//input[@tabindex='7']" << userInfo.Password
    "//input[@tabindex='8']" << userInfo.ConfirmPass
    click "div.modal-footer>button:nth-child(2)"    
    try
        waitForElementPresent "#notification-dialog"
    with _ ->
        waitForAjax()
    afterRegisterCallback()
    printfn "  User %A is created with display name %A" userInfo.UserName (userInfo.DisplayName + " User")

let loginAsRegisteredUser username password = 
    let ru = RegisteredUser(username, password)
    loginPopupAs ru

/// <summary>Deletes a user in the PB Users section</summary>
/// <param name="username">Username of the user to be deleted</param>
let deleteUser username =
    openPBUsers()
    let searchTB = "div.users-filter-container>div>div>div>input"
    let userName = sprintf "//p[.='%s']" username
    //if username not visible, search for user
    if not(existsAndVisible userName) then
        searchTB << username
        waitForAjax()
        waitForElementPresent userName
    let userEllipsis = userName + "/../../div/div[not(@title)]/div/*"
    let deleteLink = userEllipsis + "/../../div[contains(@class,'dnn-user-menu')]/ul/li[contains(.,\"" + deleteUserText + "\")]"
    click userEllipsis
    waitForElementPresent deleteLink
    click deleteLink
    let confBtn = "a#confirmbtn"
    waitForElementPresent confBtn
    click confBtn
    waitForAjax()
