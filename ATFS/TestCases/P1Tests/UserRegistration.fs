module UserRegistration

open canopy
open DnnCanopyContext
open DnnUserLogin
open DnnAddUser
open DnnAdmin

let private innerCreateUser userName displayName pwd email = 
    logOff()
    let userInfo = 
        {   UserName = userName
            Password = pwd
            ConfirmPass = pwd
            DisplayName = displayName
            EmailAddress = email }
    registerUser userInfo ignore

let private innerCreateUserAndApprove userName displayName pwd email = 
    innerCreateUser userName displayName pwd email
    approveUser userName Host

let testRegistrationSucceeded ( username : string ) ( pwd : string ) =
    loginAsRegisteredUser username pwd
    waitPageLoad()
    if not (existsAndVisible userDisplayNameSelector) then failwith "User Registration failed."

/// <summary>Testcase helper for to verify the Required Validators and Password Strength Meter exist on User Registration Form</summary>
let private testUserRegistrationFormUI()=
    logOff()
    clickDnnPopupLink siteSettings.registerLinkId
    click registerNewUserBtn
    waitForAjax()
    let requiredValidators = (element "#dnn_ctr_Register_RegistrationForm") |> elementsWithin "//span[contains(@id,'Required')]"
    if requiredValidators.Length < 5 then 
        failwith "  FAIL: One or more Required Validators is missing on the User Registration Form"
    closePopup()
    clickDnnPopupLink siteSettings.registerLinkId
    "//div[contains(@id,'passwordContainer')]/input" << defaultPassword
    waitForAjax()
    if not(existsAndVisible "//div[@class='meter visible']") then 
        failwith "  FAIL: Password Strength Meter is missing on the User Registration Form"
    closePopup()

let UITests _ =
    context "Registering a User: UI Tests"

    "Registering a User UI Test | Verify Validators and Password Strength Meter exist" @@@ fun _ ->
        testUserRegistrationFormUI()

let privateReg _ =
    context "Registering a User: Private Registration"

    "Private Registration | Normal" @@@ fun _ ->
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        changeUserRegistrationType UserRegistrationType.PRIVATE
        closePersonaBar()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false       
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        let email = "test" + random + "@test.com"
        innerCreateUserAndApprove username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Private Registration | Long Username" @@@ fun _ ->
        let random = getRandomId()
        //Username field size limit in database is 100 chars
        let username = longUserName.Substring(0,96) + random
        let dispname = longUserName.Substring(0,96) + " " + random
        let email = "test" + random + "@test.com"
        innerCreateUserAndApprove username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Private Registration | Long Password" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        //Max char limit on UI pwd field is 39 chars
        let pwd = longPassword.Substring(0,39)
        let email = "test" + random + "@test.com"
        innerCreateUserAndApprove username dispname pwd email
        testRegistrationSucceeded username pwd

    "Private Registration | Long Display Name" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        //Displayname limit is 100 chars (In DB, it is set to 128)
        let dispname = longUserName.Substring(0,95) + " " + random
        let email = "test" + random + "@test.com"
        innerCreateUserAndApprove username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Private Registration | Long Email Address" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        let email = longUserName.Substring(0,243) + random + "@test.com"
        innerCreateUserAndApprove username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword        

let publicReg _ =
    context "Registering a User: Public Registration"

    "Public Registration | Normal" @@@ fun _ ->
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        changeUserRegistrationType UserRegistrationType.PUBLIC
        closePersonaBar()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false        
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        let email = "test" + random + "@test.com"
        innerCreateUser username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Public Registration | Long Username" @@@ fun _ ->
        let random = getRandomId()
        //Username field size limit in database is 100 chars
        let username = longUserName.Substring(0,96) + random
        let dispname = longUserName.Substring(0,96) + " " + random
        let email = "test" + random + "@test.com"
        innerCreateUser username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Public Registration | Long Password" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        //Max char limit on UI pwd field is 39 chars
        let pwd = longPassword.Substring(0,39)
        let email = "test" + random + "@test.com"
        innerCreateUser username dispname pwd email
        testRegistrationSucceeded username pwd

    "Public Registration | Long Display Name" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        //Displayname limit is 100 chars (In DB, it is set to 128)
        let dispname = longUserName.Substring(0,95) + " " + random
        let email = "test" + random + "@test.com"
        innerCreateUser username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Public Registration | Long Email Address" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        let email = longUserName.Substring(0,243) + random + "@test.com"
        innerCreateUser username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword        

let verifiedReg _ =
    context "Registering a User: Verified Registration"

    "Verified Registration | Normal" @@@ fun _ ->
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        changeUserRegistrationType UserRegistrationType.VERIFIED
        closePersonaBar()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false        
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        let email = "test" + random + "@test.com"
        innerCreateUser username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Verified Registration | Long Username" @@@ fun _ ->
        let random = getRandomId()
        //Username field size limit in database is 100 chars
        let username = longUserName.Substring(0,96) + random
        let dispname = longUserName.Substring(0,96) + " " + random
        let email = "test" + random + "@test.com"
        innerCreateUser username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Verified Registration | Long Password" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        //Max char limit on UI pwd field is 39 chars
        let pwd = longPassword.Substring(0,39)
        let email = "test" + random + "@test.com"
        innerCreateUser username dispname pwd email
        testRegistrationSucceeded username pwd

    "Verified Registration | Long Display Name" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        //Displayname limit is 100 chars (In DB, it is set to 128)
        let dispname = longUserName.Substring(0,95) + " " + random
        let email = "test" + random + "@test.com"
        innerCreateUser username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword

    "Verified Registration | Long Email Address" @@@ fun _ ->
        let random = getRandomId()
        let username = "User" + random
        let dispname = "User " + random
        let email = longUserName.Substring(0,243) + random + "@test.com"
        innerCreateUser username dispname defaultPassword email
        testRegistrationSucceeded username defaultPassword
        //Set User Registration Type back to Private for tests that will follow
        changeUserRegistrationType UserRegistrationType.PRIVATE

let all _ =
    UITests()
    privateReg()
    publicReg()
    verifiedReg()
