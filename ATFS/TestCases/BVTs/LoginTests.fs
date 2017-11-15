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
