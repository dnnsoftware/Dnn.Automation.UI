module Experimental

open System.IO
open canopy
open DnnCanopyContext
open DnnUserLogin

let uploadTest _ =
    context "File Upload"
    "File Upload test" @@@ fun _ ->
        loginAsHost()
        openPBExtenstions()
        let uploadInput = "#dropzoneId>div>div>input"
        let nextBtn = "div.modal-footer>button:nth-child(2)"
        click installExtBtn
        waitForElementPresent nextBtn
        waitForAjax()
        let zipPath = Path.Combine(additionalFilesLocation, "Dnn.PersonaBar.HelloWorld_01.00.00_Install.zip")
        uploadInput << (fixfilePath zipPath)
        waitForAjax()
        if existsAndVisible "div.already-installed-container>p.repair-or-install" then
            failwithf "  ERROR: Module %A is already installed." "testmodule"
        waitForElementPresent "//div[@class='upload-percent' and .='100%']"

let all _ =
    uploadTest()
