// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
// Copyright (c) 2015 - 2017, DNN Corporation
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

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
let private testPositiveUserRegistrationFormUI()=
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

let PositiveUITests _ =
    context "Registering a User: Positive UI Tests"

    "Registering a User Pisitive UI Test | Verify Validators and Password Strength Meter exist" @@@ fun _ ->
        testPositiveUserRegistrationFormUI()

let NegativeUITests _ =
    context "Registering a User: Negative UI Tests"

    "Registering a User Negative UI Test | Verify Invalid Name Is Not Passed" @@@ fun _ ->
        logOff()
        clickDnnPopupLink "#dnn_dnnUser_enhancedRegisterLink"
        let inputs = element "#dnn_ctr_Register_userForm" |> elementsWithin "input"
        let name = "malicioususer" +  getRandomId()
        inputs.Item(0) << name + "<script>alert(1)</script>" // username
        inputs.Item(1) << config.Site.DefaultPassword
        inputs.Item(2) << config.Site.DefaultPassword
        inputs.Item(3) << name
        inputs.Item(4) << name + "@dnndev.me"
        click "#dnn_ctr_Register_registerButton"
        waitPageLoad()
        // once this is translated, we need to change this
        let errorText = 
            match installationLanguage with
            | English -> "The username specified is invalid.  Please specify a valid username."
            | German  -> "Der angegebene Benutzername ist ungültig, bitte geben Sie einen gültigen Benutzernamen an."
            | Spanish -> "El nombre de usuario no es correcto. Especifique un nombre de usuario válido."
            | French  -> "Compte utilisateur incorrect. Veuillez fournir un compte valide."
            | Italian -> "Il nome utente specificato non è valido. Si prega di fornire un account valido."
            | Dutch   -> "De gebruikersnaam is niet correct. Voer alstublieft een correcte gebruikersnaam in."
        displayed ( sprintf "#dnn_ctr_ctl01_dnnSkinMessage > span:contains(%s)" errorText )
        ()

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
    PositiveUITests()
    NegativeUITests()
    privateReg()
    publicReg()
    verifiedReg()
