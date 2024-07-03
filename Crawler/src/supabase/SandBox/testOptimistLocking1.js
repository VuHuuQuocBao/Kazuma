import { AuthWeakPasswordError, createClient } from "@supabase/supabase-js"
import { config } from "dotenv"
import { supabaseClient } from "../supabaseClient.js"

export async function getOldestRecords(supabaseClient, limit) {
    let { data, error } = await supabaseClient.from("MangaInfoGeneric").select("*").eq("Lock", "false").order("updatedAt", { ascending: true }).limit(limit)

    function delay(ms) {
        return new Promise((resolve) => setTimeout(resolve, ms))
    }

    await delay(10000)

    var listIds = []

    data.forEach((item) => {
        item.Lock = "true"
        listIds.push(item.id)
    })

    const { status } = await supabaseClient.from("MangaInfoGeneric").upsert(data)
    if (status == "200") {
        let { data, error } = await supabaseClient.from("MangaInfoGeneric").select("*").in("id", listIds)
        console.log(data)
    } else {
        console.log("Trigger Activated")
        console.error(error)
    }

    if (error) console.error(error)
    return data
}

getOldestRecords(supabaseClient, 20)
