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
