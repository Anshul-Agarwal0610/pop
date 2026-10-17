import fs from "node:fs/promises"
import path from "node:path"
import sharp from "sharp"

const root = path.resolve(import.meta.dirname, "..")
const repo = path.resolve(root, "..")
const mark = await fs.readFile(path.join(root, "public/brand/pop-mark.svg"))
const mono = Buffer.from(mark.toString().replaceAll("#FF6B61", "#FFFFFF").replaceAll("#8D73F6", "#FFFFFF"))
const mobile = path.join(repo, "apps/mobile/assets")
const web = path.join(root, "public")
await fs.mkdir(path.join(web, "icons"), { recursive: true })

async function canvas(size, background, symbol, symbolSize, output, rounded = false) {
  const base = sharp({ create: { width: size, height: size, channels: 4, background } })
  const resized = await sharp(symbol).resize(symbolSize, symbolSize, { fit: "contain" }).png().toBuffer()
  let result = base.composite([{ input: resized, gravity: "center" }])
  if (rounded) {
    const radius = Math.round(size * 0.22)
    const mask = Buffer.from(`<svg width="${size}" height="${size}"><rect width="${size}" height="${size}" rx="${radius}" fill="white"/></svg>`)
    result = result.composite([{ input: mask, blend: "dest-in" }])
  }
  await result.png().toFile(output)
}

await canvas(1024, "#17151F", mark, 700, path.join(mobile, "icon.png"))
await canvas(1024, { r: 0, g: 0, b: 0, alpha: 0 }, mark, 600, path.join(mobile, "android-icon-foreground.png"))
await sharp({ create: { width: 1024, height: 1024, channels: 4, background: "#17151F" } }).png().toFile(path.join(mobile, "android-icon-background.png"))
await canvas(1024, { r: 0, g: 0, b: 0, alpha: 0 }, mono, 600, path.join(mobile, "android-icon-monochrome.png"))
await canvas(1024, { r: 0, g: 0, b: 0, alpha: 0 }, mark, 560, path.join(mobile, "splash-icon.png"))
await canvas(512, { r: 0, g: 0, b: 0, alpha: 0 }, mark, 512, path.join(mobile, "pop-mark.png"))
await canvas(512, { r: 0, g: 0, b: 0, alpha: 0 }, await fs.readFile(path.join(web, "brand/public-pulse-mark.svg")), 512, path.join(mobile, "public-pulse-mark.png"))
await canvas(64, "#17151F", mark, 46, path.join(mobile, "favicon.png"), true)

await canvas(180, "#17151F", mark, 128, path.join(web, "apple-icon.png"), true)
await canvas(32, "#FAF7F2", mark, 25, path.join(web, "icon-light-32x32.png"), true)
await canvas(32, "#17151F", mark, 25, path.join(web, "icon-dark-32x32.png"), true)
await canvas(192, "#17151F", mark, 132, path.join(web, "icons/icon-192.png"))
await canvas(512, "#17151F", mark, 352, path.join(web, "icons/icon-512.png"))
await canvas(1024, { r: 0, g: 0, b: 0, alpha: 0 }, mark, 1024, path.join(web, "brand/pop-mark.png"))
await canvas(1024, { r: 0, g: 0, b: 0, alpha: 0 }, await fs.readFile(path.join(web, "brand/public-pulse-mark.svg")), 1024, path.join(web, "brand/public-pulse-mark.png"))

console.log("PoP brand assets generated.")
