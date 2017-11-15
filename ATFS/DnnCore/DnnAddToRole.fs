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

module DnnAddToRole

open canopy
open DnnAddUser

let private getRandomId() = getRandomId()

// Note: a manager user should already be logged in before calling this function
let addUserToRole username (rolename : string) = 
    openPBUsers()
    let searchTB = "div.users-filter-container>div>div>div>input"  
    let userManageRoles = sprintf "//p[contains(.,'%s')]/../../div[4]/div[4]/*" username
    if not(existsAndVisible userManageRoles) then
        searchTB << username
        waitForAjax()
        waitForElementPresent userManageRoles 
    click userManageRoles
    waitForAjax()
    sleep 0.2
    let searchRoleTB = "div.add-box>div>span>div>input"
    try    
        waitForElementPresent searchRoleTB
    with _ ->
        sleep 1
        click userManageRoles
        waitForAjax()
        sleep 0.2
        waitForElementPresent searchRoleTB
    try
        searchRoleTB << rolename
        waitForAjax()
        waitForElementPresent (sprintf "//li[.='%s']" rolename)
    with _ ->
        searchRoleTB << rolename
        waitForAjax()
        waitForElementPresent (sprintf "//li[.='%s']" rolename)
    (element searchRoleTB).SendKeys(enter)
    waitForAjax()
    click "div.add-role-button"
    let roleDiv = sprintf "//div[.='%s']" rolename
    waitForElementPresent roleDiv

let loginAsHost() =
    maximizeWindow()
    loginAsRegisteredUser hostUsername defaultPassword

let private createAdmin() =
    let id = getRandomId()
    let userInfo = 
        { UserName = "admin" + id
          Password = defaultPassword
          ConfirmPass = defaultPassword
          DisplayName = "Administrator " + id
          EmailAddress = "admin" + id + "@change.me" }
    registerUserAsHost userInfo ignore
    addUserToRole userInfo.UserName "Administrators"
    userInfo

let mutable private siteAdmin : RegisterUserInfo = 
    { UserName = ""
      Password = ""
      ConfirmPass = ""
      DisplayName = ""
      EmailAddress = "" }

let loginAsAdmin() =
    if siteAdmin.UserName = "" then
        siteAdmin <- createAdmin()
    loginAsRegisteredUser siteAdmin.UserName defaultPassword
    siteAdmin

let registerUserAsAdmin (userInfo : RegisterUserInfo) afterRegisterCallback = 
    loginAsAdmin() |> ignore
    openPBUsers()
    let addUserBtn = sprintf "//button[.=\"%s\"]" addUserText
    click addUserBtn
    let newUserInput = "div.new-user-box>div>div>div>div>div>input[tabindex='NUM']"
    newUserInput.Replace("NUM","1") << userInfo.DisplayName
    newUserInput.Replace("NUM","2") << "User"
    newUserInput.Replace("NUM","3") << userInfo.UserName
    newUserInput.Replace("NUM","4") << userInfo.EmailAddress
    newUserInput.Replace("NUM","7") << userInfo.Password
    newUserInput.Replace("NUM","8") << userInfo.ConfirmPass
    click "div.modal-footer>button[role=primary]" //save btn
    try
        waitForElementPresent "#notification-dialog"
    with _ ->
        waitForAjax()
    afterRegisterCallback()
    printfn "  User %A is created with display name %A" userInfo.UserName (userInfo.DisplayName + " User")

let private createUserAndAddRole userName displayName roleName = 
    let userInfo = 
        { UserName = userName
          Password = defaultPassword
          ConfirmPass = defaultPassword
          DisplayName = displayName
          EmailAddress = userName + "@test.com" }
    registerUserAsAdmin userInfo ignore
    if roleName <> "Registered Users" then
        addUserToRole userInfo.UserName roleName
    userInfo

let mutable private regularUser : RegisterUserInfo = 
    { UserName = ""
      Password = ""
      ConfirmPass = ""
      DisplayName = ""
      EmailAddress = "" }

let loginAsRegularUser() =
    let roleName = "Registered Users"
    if regularUser.UserName = "" then
        let id = getRandomId()
        let userName = "reguser" + id
        let displayName = "Regular User " + id
        regularUser <- createUserAndAddRole userName displayName roleName
    loginAsRegisteredUser regularUser.UserName defaultPassword  
    regularUser

/// <summary>Clears the set of stored user credentials</summary>
let clearStoredUsers()=
    let clearUser : RegisterUserInfo = 
        { UserName = ""
          Password = ""
          ConfirmPass = ""
          DisplayName = ""
          EmailAddress = "" }
    siteAdmin <- clearUser
    regularUser <- clearUser
