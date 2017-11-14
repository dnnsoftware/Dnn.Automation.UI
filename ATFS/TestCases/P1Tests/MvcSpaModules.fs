module MvcSpaModules

open System.IO
open DnnCanopyContext
open DnnExtensions
open DnnAddModuleToPage
open DnnMvcSpa
open DnnUserLogin

let private modulePathMVC = Path.Combine(additionalFilesLocation, "DNN_ContactList_Mvc_01.00.01_Install.zip")
let private modulePathSPA = Path.Combine(additionalFilesLocation, "DNN_ContactList_SPA_01.00.00_Install.zip")
let mutable private moduleIdMvc = 0
let mutable private moduleIdSpa = 0

let private mvcModuleTests _ =

    context "MVC Module: Install and Test"

    "MVC Module | Install and Deploy on a page" @@@ fun _ ->
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        loginAsHost()
        let moduleName = "Contact List Mvc"
        installHostExtension modulePathMVC moduleName
        let modId, pgUrl = deployModuleOnPage false "DnnContactListMvc" moduleName null
        moduleIdMvc <- modId
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "MVC Module | Add New Contact" @@@ fun _ ->
        addNewContactMVC moduleIdMvc ("firstnamemvc"+getRandomId()) ("lastnamemvc"+getRandomId())

    "MVC Module | Check PartialView and Json calls change the time" @@@ fun _ ->
        checkPartialAndJsonCallsChangeTimeMVC moduleIdMvc

    context "MVC Module: Uninstall"

    "MVC Module | Uninstall Extension" @@@ fun _ ->
        logOff()
        loginAsHost()
        uninstallHostExtension "Modules" "Contact List Mvc"

let private spaModuleTests _ =

    context "SPA Module: Install and Test"

    "SPA Module | Install and Deploy on a page" @@@ fun _ ->
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        logOff()
        loginAsHost()
        let moduleName = "Contact List Spa"
        installHostExtension modulePathSPA moduleName
        let modId, pgUrl = deployModuleOnPage false "DnnContactListSpa" moduleName null
        moduleIdSpa <- modId
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "SPA Module | Add New Contact" @@@ fun _ ->
        addNewContactSPA moduleIdSpa ("firstnamespa"+getRandomId()) ("lastnamespa"+getRandomId())

    context "SPA Module: Uninstall"

    "SPA Module | Uninstall Extension" @@@ fun _ ->
        logOff()
        loginAsHost()
        uninstallHostExtension "Modules" "Contact List Spa"
        logOff()

let all _ =
    mvcModuleTests()
    spaModuleTests()
