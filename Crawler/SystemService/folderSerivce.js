import path from "path"
import fs from "fs"

function getDirectorySize(directoryPath) {
    const files = fs.readdirSync(directoryPath)
    let size = 0
    let imageCount = 0

    files.forEach((file) => {
        const filePath = path.join(directoryPath, file)
        const stats = fs.statSync(filePath)

        // Check if file is a directory
        if (stats.isDirectory()) {
            size += getDirectorySize(filePath)
        } else {
            size += stats.size

            // Check if file is an image
            if ([".jpg", ".jpeg", ".png", ".gif"].includes(path.extname(file))) {
                imageCount++
            }
        }
    })

    return { size: size / 1024, imageCount } // size in KB
}

const directoryPath = "C:\\Users\\PC\\OneDrive\\Desktop\\anime\\FaceDectection2\\yolov5_anime\\input" // replace with your directory path

// const result = getDirectorySize(directoryPath)
// console.log(`Size of directory: ${result.size.toFixed(2)} KB`)
// console.log(`Number of images: ${result.imageCount}`)
