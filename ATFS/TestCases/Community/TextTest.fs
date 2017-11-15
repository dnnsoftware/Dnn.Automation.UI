module TextTest

open canopy
open DnnCanopyContext

let positive _ =
    
    context "Testing Text in Pages"

    "Home Page Contains 'Every Journey ...'" @@@ fun _ -> 
        goto "/"
        let el = element "#dnn_HeaderPane" |> elementWithin "h2"
        Check.Contains el.Text "Every journey begins with the first step."

let all _ =
    positive()
