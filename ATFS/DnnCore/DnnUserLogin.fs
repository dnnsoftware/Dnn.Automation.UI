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

module DnnUserLogin

open canopy

//helpers
let private loginPage = "/Login"
let private logoffPage = "?ctl=Logoff"
let private dontShowAgainCbox = "//input[@name='ShowDialog']"
let private closeDialogButton = "//button[@class='ui-button ui-widget ui-state-default ui-corner-all ui-button-icon-only ui-dialog-titlebar-close']"

let mutable lastLoggedinUser = ""
let mutable lastLoggedinUserPwd = ""

let logOff() = 
    goto logoffPage
    lastLoggedinUser <- ""
    if existsAndVisible siteSettings.loggedinUserImageLinkId then 
        try 
            if siteSettings.logoutLinkIsDropDownMenu then 
                click siteSettings.loggedinUserNameLinkId
                sleep 0.2
            click siteSettings.logoutLinkId
            waitPageLoad()
        with _ -> goto logoffPage

let dismissWelcomePopup() = 
    waitPageLoad() // wait for the welcome page when visible
    if exists dontShowAgainCbox  || existsAndVisible closeDialogButton then 
        let e = element dontShowAgainCbox
        clickCboxLabel (e.GetAttribute("id"))
        if existsAndVisible welcomeToSiteText then 
            click welcomeToSiteText // make it active before click ESC
        let btn = elements closeDialogButton |> List.tryFind (fun e -> e.Displayed)
        match btn with
        | Some(b) -> click b
        | None -> press esc // not working all the times as expected
        waitForAjax()

// logs in and returns true if successful; false oherwise
let public doLogin (user : DnnUser) isPopup = 
    let login u p = 
        if u <> lastLoggedinUser then
            logOff()
            if isPopup then clickDnnPopupLink siteSettings.loginLinkId
            else goto loginPage
            loginUserNameTextBoxId << u
            loginPasswordTextBoxId << p
            press enter //click loginButtonId
            waitPageLoad()
            printfn "  Loggged in as %A" u
            closeToastNotification()
        let success = existsAndVisible siteSettings.loggedinUserImageLinkId
        if success then 
            lastLoggedinUser <- u
            lastLoggedinUserPwd <- p
        success

    match user with
    | Host -> 
        let r = login hostUsername defaultPassword
        dismissWelcomePopup()
        r
    | RegisteredUser(u, p) -> login u p

// logs in to page using popup
// returns true if successful; false oherwise
let loginPopupAs (user : DnnUser) = doLogin user true |> ignore

// logs in to page using non-popup ligin page
// returns true if successful; false oherwise
let loginOnPageAs (user : DnnUser) = doLogin user false |> ignore

// logs in as the HOST user
let loginAsHost() = loginPopupAs Host