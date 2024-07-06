export async function getOldestRecords(supabaseClient, limit) {
    let { data, error } = await supabaseClient.from("MangaInfoGeneric").select("*").eq("processed", "true").order("updatedAt", { ascending: true }).limit(limit)

    if (error) console.error(error)
    return data
}

export async function upsertSupabase(supabaseClient, updateArray) {
    const { data, error } = await supabaseClient.from("MangaInfoGeneric").upsert(updateArray)
}

export async function getDataForChapterCrawler(supabaseClient, limit) {
    const { data, error } = await supabaseClient.from("MangaInfoGeneric").select("*").not("current_chapter", "is", "null").order("updatedAt", { ascending: true }).limit(limit)

    if (error) console.error(error)
    var threshold = 0
    var result = []

    for (var i = 0; i < data.length; i++) {
        if (threshold + data[i].current_chapter > 100) break
        else threshold += data[i].current_chapter

        result.push(data[i])
    }
    return result
}

export async function CheckOldestRecordsLocking(supabaseClient, limit) {
    let { data, error } = await supabaseClient.from("MangaInfoGeneric").select("*").eq("Lock", "false").not("current_chapter", "is", "null").order("current_chapter", { ascending: true }).limit(limit)

    if (error) {
        console.error(error)
        return []
    }
    return data
}

export async function getOldestRecordsLocking(supabaseClient, limit) {
    let { data, error } = await supabaseClient.from("MangaInfoGeneric").select("*").eq("Lock", "false").not("current_chapter", "is", "null").order("updatedAt", { ascending: false }).limit(limit)

    if (error) {
        console.error(error)
        return []
    }
    const threshold = 20
    var listIds = []

    data.forEach((item) => {
        item.Lock = "true"
        listIds.push(item.id)
    })

    const { status: upserStatus, error: upsertError } = await supabaseClient.from("MangaInfoGeneric").upsert(data)
    if (upsertError) {
        console.error("Error upserting records:", upsertError)
        return []
    }

    if (upserStatus == "200") {
    } else {
        console.log("Trigger Activated")
        return []
    }

    for (const item of data) {
        if (item.current_chapter > threshold) {
            return [] // Immediately return [] if the condition is met
        }
    }

    return data
}
