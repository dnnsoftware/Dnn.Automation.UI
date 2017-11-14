module DnnUserProfile

open canopy

let gotoEditProfilePage() =
    goto "/Activity-Feed"
    click "div.UserProfileControls>ul>li:first-of-type>a"
    waitPageLoad()

/// <summary>Updates the user profile by click Update Profile button</summary>
let updateProfile()=
    let updateBtn = "//ul[contains(@id,'EditUser_Profile')]/li/a[contains(@id,'cmdUpdate')]"
    scrollTo updateBtn
    click updateBtn
    sleep 0.5
    waitPageLoad()

let expandManageAccountPage() = 
    printfn "  INFO: Expanding Section Headers on the Manage Accounts Page"
    let sectionHeaders = [ "Account Settings"; "Manage Password"; "Account Information" ]  
    expandSectonHeaders sectionHeaders

let expandManageProfilePage() = 
    printfn "  INFO: Expanding Section Headers on the Manage Profile Page"
    try
        expandSectonHeaders [ "Basic"; "Contact"; "Location" ]
    with _ ->
        expandSectonHeaders [ "Name"; "Address"; "Contact Info"; "Preferences" ]

//User should already be on the Edit Profile page
let updateAccountProperties displayname emailaddress =
    click "//a[contains(text(),'Manage Account')]"
    waitForAjax()
    expandManageAccountPage()
    if displayname <> "" then
        "#dnn_ctr_EditUser_userForm_displayName_displayName_TextBox" << displayname
    if emailaddress <> "" then
        "#dnn_ctr_EditUser_userForm_email_email_TextBox" << emailaddress
    updateProfile()

//User should already be on the Edit Profile page
let updateProfileProperties firstname lastname streetaddress city cellnumber =
    click "//a[contains(text(),'Manage Profile')]"
    waitForAjax()
    expandManageProfilePage()
    if firstname <> "" then
        "#dnn_ctr_EditUser_Profile_ProfileProperties_FirstName_FirstName" << firstname
    if lastname <> "" then
        "#dnn_ctr_EditUser_Profile_ProfileProperties_LastName_LastName" << lastname
    scrollTo "#dnn_ctr_EditUser_Profile_ProfileProperties_Street_Street"
    if streetaddress <> "" then
        "#dnn_ctr_EditUser_Profile_ProfileProperties_Street_Street" << streetaddress
    if city <> "" then
        "#dnn_ctr_EditUser_Profile_ProfileProperties_City_City" << city
    if cellnumber <> "" then
        "#dnn_ctr_EditUser_Profile_ProfileProperties_Cell_Cell" << cellnumber
    updateProfile()

/// <summary>Gets the source path for the Profile Picture</summary>
let getProfilePictureSrc()=
    let profImg = "img[alt='Profile Avatar']"
    (element profImg).GetAttribute("src")

/// <summary>Opens the Profile Picture section in Edit Profile</summary>
let openProfilePicSection()=
    gotoEditProfilePage()
    click "//a[contains(@href,'dnnProfileDetails')]" //manage profile tab
    waitForAjax()
    expandManageProfilePage()        
    scrollTo uploadProfilePicBtn
    if not(existsAndVisible uploadProfilePicBtn) then
        sleep 0.5
        scrollTo uploadProfilePicBtn
    waitForAjax()

/// <summary>Upload a profile picture (avatar) for the logged-in user</summary>
/// <param name="filePath">Path of the profile picture</param>
let addProfilePicture filePath =
    openProfilePicSection()
    try 
        click uploadProfilePicBtn
    with _ ->
        scrollTo uploadProfilePicBtn
        click uploadProfilePicBtn
    let chooseFile = "span.dnnInputFileWrapper>input"
    waitForElementPresent "div.fu-container" //upload dialog
    chooseFile << (fixfilePath filePath)
    waitForElementPresent "div.ui-progressbar-value[style*='100%']" //file upload 100%
    click "div.ui-dialog-buttonset>button" //close button
    waitForAjax()
    updateProfile()

