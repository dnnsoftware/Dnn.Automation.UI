﻿module SiteSettings

open System.IO
open canopy
open DnnCanopyContext
open DnnAddToRole
open DnnSiteSettings

let private mainTests _ =
    context "Site Settings | Site Logo and Favicon"

    "Site Settings | Changing Site Logo by uploading new image" @@@ fun _ ->
        loginAsAdmin() |> ignore
        let logoName = @"altlogo.png"
        let filePath = Path.Combine(additionalFilesLocation, logoName)
        changeSiteLogoNew logoName filePath
        closePersonaBar()
        reloadPage()
        verifySiteLogo logoName

    "Site Settings | Changing Site Logo by choosing existing image" @@@ fun _ ->
        let logoName = "logo.png"
        let logoFolder = "Images"
        changeSiteLogoExisting logoName logoFolder
        closePersonaBar()
        reloadPage()
        verifySiteLogo logoName

    "Site Settings | Changing Favicon by uploading new image" @@@ fun _ ->
        loginAsAdmin() |> ignore
        let faviconName = @"dnn.ico"
        let filePath = Path.Combine(additionalFilesLocation, faviconName)
        changeFaviconNew faviconName filePath
        closePersonaBar()
        reloadPage()
        verifyFavicon faviconName

    "Site Settings | Changing Favicon by choosing existing image" @@@ fun _ ->
        let iconName = @"amazon.ico"
        let filePath = Path.Combine(additionalFilesLocation, iconName)
        changeFaviconNew iconName filePath
        closePersonaBar()
        reloadPage()
        verifyFavicon iconName
        //choose existing image
        let existingIconName = @"dnn.ico"
        let existingIconFolder = @"Site Root"
        changeFaviconExisting existingIconName existingIconFolder
        closePersonaBar()
        reloadPage()
        verifyFavicon existingIconName    

    "Site Settings | Auto Add Site Alias by Default Disabled" @@@ fun _ ->
        loginAsHost()
        openPBSiteSettings()
        let siteSettingsTabs = "div.siteSettings-app>div>div>div>div"
        click (siteSettingsTabs + ">ul>li:nth-of-type(2)") //Site Behavior tab
        waitForAjax()
        click (siteSettingsTabs + ">div>div>ul>li:nth-of-type(4)") //Site Aliases tab
        waitForAjax()
        if exists "div.urlMappingSettings-row_switch>div>span.dnn-switch-active"  then 
            failwith "  FAIL: Auto Add Site Alias Toggle is enabled by default"

let all _ = 
    mainTests()
