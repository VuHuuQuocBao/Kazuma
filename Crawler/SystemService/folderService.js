import path from "path"
import fs from "fs/promises"

export const getDirectorySize = async (directoryPath) => {
    const files = await fs.readdir(directoryPath)
    let size = 0
    let imageCount = 0

    for (const file of files) {
        const filePath = path.join(directoryPath, file)
        const stats = await fs.stat(filePath)

        // Check if file is a directory
        if (stats.isDirectory()) {
            const subdirResult = await getDirectorySize(filePath)
            size += subdirResult.size
            imageCount += subdirResult.imageCount
        } else {
            size += stats.size

            // Check if file is an image
            if ([".jpg", ".jpeg", ".png", ".gif"].includes(path.extname(file))) {
                imageCount++
            }
        }
    }

    return { size: size, imageCount: imageCount } // size in bytes
}

// const directoryPath = "C:\\Users\\PC\\OneDrive\\Desktop\\Project\\Yolov5\\input"

// getDirectorySize(directoryPath)
//     .then((result) => {
//         console.log(`Size of directory: ${(result.size / 1024 / 1024).toFixed(2)} MB`)
//         console.log(`Number of images: ${result.imageCount}`)
//     })
//     .catch(console.error)
