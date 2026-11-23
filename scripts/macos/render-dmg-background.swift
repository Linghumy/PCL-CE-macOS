#!/usr/bin/env swift

import AppKit

guard CommandLine.arguments.count >= 3,
      let scale = Double(CommandLine.arguments[2]),
      scale > 0
else {
    fputs("usage: render-dmg-background.swift <output.png> <scale>\n", stderr)
    exit(2)
}

let outputPath = CommandLine.arguments[1]
let pointWidth = 680.0
let pointHeight = 420.0
let pixelWidth = Int(pointWidth * scale)
let pixelHeight = Int(pointHeight * scale)

guard let bitmap = NSBitmapImageRep(
    bitmapDataPlanes: nil,
    pixelsWide: pixelWidth,
    pixelsHigh: pixelHeight,
    bitsPerSample: 8,
    samplesPerPixel: 4,
    hasAlpha: true,
    isPlanar: false,
    colorSpaceName: .deviceRGB,
    bytesPerRow: 0,
    bitsPerPixel: 0
) else {
    fputs("unable to create bitmap\n", stderr)
    exit(1)
}

bitmap.size = NSSize(width: pointWidth, height: pointHeight)
guard let context = NSGraphicsContext(bitmapImageRep: bitmap) else {
    fputs("unable to create graphics context\n", stderr)
    exit(1)
}

NSGraphicsContext.saveGraphicsState()
NSGraphicsContext.current = context

let fullRect = NSRect(x: 0, y: 0, width: pointWidth, height: pointHeight)
let background = NSGradient(colors: [
    NSColor(calibratedRed: 0.965, green: 0.976, blue: 0.992, alpha: 1),
    NSColor(calibratedRed: 0.902, green: 0.933, blue: 0.976, alpha: 1)
])
background?.draw(in: fullRect, angle: -90)

let borderPath = NSBezierPath(roundedRect: fullRect.insetBy(dx: 0.5, dy: 0.5), xRadius: 18, yRadius: 18)
NSColor(calibratedWhite: 1, alpha: 0.48).setStroke()
borderPath.lineWidth = 1
borderPath.stroke()

func font(_ name: String, size: CGFloat, weight: NSFont.Weight = .regular) -> NSFont {
    NSFont(name: name, size: size)
        ?? NSFont.systemFont(ofSize: size, weight: weight)
}

func drawCentered(
    _ text: String,
    rect: NSRect,
    font: NSFont,
    color: NSColor,
    lineSpacing: CGFloat = 2
) {
    let paragraph = NSMutableParagraphStyle()
    paragraph.alignment = .center
    paragraph.lineSpacing = lineSpacing
    let attributes: [NSAttributedString.Key: Any] = [
        .font: font,
        .foregroundColor: color,
        .paragraphStyle: paragraph
    ]
    (text as NSString).draw(
        in: rect,
        withAttributes: attributes
    )
}

drawCentered(
    "PCL-CE-macOS",
    rect: NSRect(x: 32, y: 366, width: 616, height: 34),
    font: font("PingFang SC", size: 24, weight: .semibold),
    color: NSColor(calibratedRed: 0.13, green: 0.23, blue: 0.36, alpha: 1)
)

drawCentered(
    "注意！本项目仅为第三方分支，与 PCL 和 PCL-CE 官方无任何关联！",
    rect: NSRect(x: 38, y: 326, width: 604, height: 30),
    font: font("PingFang SC", size: 17, weight: .bold),
    color: NSColor(calibratedRed: 0.69, green: 0.12, blue: 0.12, alpha: 1)
)

drawCentered(
    "请勿将该版本的问题反馈到 PCL 或 PCL-CE 项目中！",
    rect: NSRect(x: 38, y: 295, width: 604, height: 27),
    font: font("PingFang SC", size: 15.5, weight: .semibold),
    color: NSColor(calibratedRed: 0.69, green: 0.12, blue: 0.12, alpha: 1)
)

let divider = NSBezierPath()
divider.move(to: NSPoint(x: 74, y: 279))
divider.line(to: NSPoint(x: 606, y: 279))
NSColor(calibratedRed: 0.52, green: 0.62, blue: 0.75, alpha: 0.28).setStroke()
divider.lineWidth = 1
divider.stroke()

let arrowPlate = NSBezierPath(
    roundedRect: NSRect(x: 282, y: 181, width: 116, height: 58),
    xRadius: 29,
    yRadius: 29
)
NSColor(calibratedWhite: 1, alpha: 0.72).setFill()
arrowPlate.fill()

let arrow = NSBezierPath()
arrow.move(to: NSPoint(x: 308, y: 210))
arrow.line(to: NSPoint(x: 374, y: 210))
arrow.move(to: NSPoint(x: 358, y: 226))
arrow.line(to: NSPoint(x: 374, y: 210))
arrow.line(to: NSPoint(x: 358, y: 194))
arrow.lineCapStyle = .round
arrow.lineJoinStyle = .round
NSColor(calibratedRed: 0.20, green: 0.43, blue: 0.76, alpha: 0.9).setStroke()
arrow.lineWidth = 5
arrow.stroke()

drawCentered(
    "拖入 Applications 文件夹即可安装",
    rect: NSRect(x: 210, y: 137, width: 260, height: 25),
    font: font("PingFang SC", size: 13, weight: .medium),
    color: NSColor(calibratedRed: 0.28, green: 0.38, blue: 0.51, alpha: 0.86)
)

drawCentered(
    "非官方第三方分支 · 请勿向 PCL / PCL-CE 官方反馈问题",
    rect: NSRect(x: 36, y: 24, width: 608, height: 22),
    font: font("PingFang SC", size: 11.5, weight: .regular),
    color: NSColor(calibratedRed: 0.35, green: 0.43, blue: 0.54, alpha: 0.82)
)

NSGraphicsContext.restoreGraphicsState()

guard let png = bitmap.representation(using: .png, properties: [:]) else {
    fputs("unable to encode PNG\n", stderr)
    exit(1)
}

try png.write(to: URL(fileURLWithPath: outputPath))
