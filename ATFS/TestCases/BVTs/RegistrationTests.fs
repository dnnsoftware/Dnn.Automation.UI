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
