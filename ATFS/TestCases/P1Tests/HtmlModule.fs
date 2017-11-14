module HtmlModule

open DnnCanopyContext
open DnnManager
open DnnHtmlModule
open DnnUserLogin
open InputSimulatorHelper

let htmlModuleTests _ =
    context "HTML Module Tests"

    "HTML Module | Host | Publish text in HTML Module" @@@ fun _ ->
        logOff()        
        loginAsHost()
        openNewPage true |> ignore //new page has HTML module already
        insertTextHTML null "Publish text in HTML Module" (loremIpsumText.Substring(0, 500)) false

let htmlCommonTests _ =
    context "HTML Common Tests"

    "HTML and HTML Pro Modules | Host | Token replacement test" @@@ fun _ ->
        logOff()
        loginAsHost()
        openNewPage true |> ignore 
        openEditMode()               
        //insert tokens
        insertTextHTML null "Token Replacement test" "[User:UserName] [User:FirstName] [User:LastName]" true
        enableTokenReplacement true 0 
        closeEditMode()
        hardReloadPage()
        let expectedText = sprintf "%s %s" config.Site.HostUserName config.Site.HostDisplayName
        verifyHtmlModuleText expectedText        

let all _ = 
    htmlModuleTests()
    htmlCommonTests()
