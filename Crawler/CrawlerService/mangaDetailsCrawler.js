import axios from "axios";
import * as cheerio from "cheerio";
import fs from "fs";
import https from "https";
import { supabaseClient } from "../src/supabase/supabaseClient.js";
import { getOldestRecords, upsertSupabase } from "../src/supabase/supabaseService.js";

const blackLists = ["manhua", "manhwa", "webtoon"];

const axiosConfig = {
    baseURL: "https://blogtruyenmoi.com/",
};

const client = axios.create(axiosConfig);

const GetData = async (path) => {
    const { data } = await client.get(path);
    return data;
};

function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

(async () => {
    const mangas = [];
    while (true) {
        try {
            const processData = await getOldestRecords(supabaseClient, 10);

            await delay(5000);

            if (processData.length == 0) break;

            for (const element of processData) {
                const parts = element.id?.split("-");
                const slugId = parts[parts.length - 1];
                const slugName = element.title;
                const path = `${slugId}/${slugName}`;
                await Process(element, path, mangas);
            }

            var updateArary = [];

            processData.forEach((element) => {
                updateArary.push({
                    id: element.id,
                    title: element.title,
                    processed: true,
                });
            });
            await upsertSupabase(supabaseClient, updateArary);

            if (mangas.length > 0) {
                console.log(mangas);
                axios
                    .post("http://localhost:5197/MangaInfoDetail", mangas, {
                        headers: {
                            "Content-Type": "application/json",
                        },
                        httpsAgent: new https.Agent({
                            rejectUnauthorized: false,
                        }),
                    })
                    .then((response) => {
                        console.log(response.data);
                    })
                    .catch((error) => {
                        console.error(error);
                    });

                mangas.splice(0, mangas.length);
            }
        } catch (error) {
            const data = error.config.url + "\n";
            console.log(data);
            fs.writeFileSync("./error.txt", data, { flag: "a+" }, (err) => {
                if (err) throw err;
                console.log("Data has been written to file successfully.");
            });

            const parts = error.config.url.split("/");

            const tempArray = [];
            tempArray.push({ id: `bl-${parts[0]}`, title: parts[1], processed: true });

            await upsertSupabase(supabaseClient, tempArray);
        }
    }
})();

const Process = async (element, path, mangas) => {
    const data = await GetData(path);

    const $ = cheerio.load(data);

    const genresHtmnl = $(".description .category");

    const genres = genresHtmnl
        .find("a")
        .map((i, el) => {
            return $(el).attr("href");
        })
        .get()
        .map((genre) => {
            const parts = genre?.split("/");
            return parts[parts.length - 1];
        });

    for (let element of genres) {
        const parts = element?.split("/");
        if (blackLists.includes(parts[parts.length - 1])) {
            return; // This will stop the function execution
        }
    }

    const genresString = genres.join("|");
    const mangaThumbnailUrlhtml = $(".thumbnail");
    const mangaThumbnail = mangaThumbnailUrlhtml.find("img").attr("src");

    const descriptionhtml = $(".description p");

    var authorResult = null;

    descriptionhtml.each((index, element) => {
        let name = $(element).text().trim();
        if (name.includes("Tác giả")) {
            authorResult = name;
            return false;
        }
    });

    var authorsString = "";

    const authorParts = authorResult?.split("\n");

    if (authorParts) {
        for (var i = 1; i < authorParts.length; i++) {
            if (i == authorParts.length - 1) authorsString += authorParts[i].trim();
            else authorsString += authorParts[i].trim() + "|";
        }
    }

    const listChaptershtml = $(".list-wrap");
    const listChaptersId = listChaptershtml
        .find("p")
        .map((i, el) => {
            return $(el).attr("id");
        })
        .get()
        .map((chapterId) => {
            const parts = chapterId?.split("-");
            return parts[parts.length - 1];
        });

    const listChaptersIdString = listChaptersId.reverse().join("|");

    mangas.push({
        Id: element.id,
        Thumbnail: mangaThumbnail,
        Author: authorsString,
        Genre: genresString,
        ListChapter: listChaptersIdString,
        CurrentChapter: listChaptersId.length.toString(),
        Title: element.title,
        CreatedAt: element.createdAt,
        UpdatedAt: element.updatedAt,
    });
};
