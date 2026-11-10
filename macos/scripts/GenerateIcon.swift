import AppKit

// Native export of the existing overlapping screens + crescent identity. No downloaded art.
guard CommandLine.arguments.count == 2 else { fatalError("Expected an .iconset output directory") }
let directory = URL(fileURLWithPath: CommandLine.arguments[1], isDirectory: true)
try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
for points in [16, 32, 128, 256, 512] {
    for scale in [1, 2] {
        let pixels = points * scale
        let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: pixels, pixelsHigh: pixels,
            bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
            colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
        NSGraphicsContext.saveGraphicsState()
        NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
        let transform = NSAffineTransform()
        transform.scale(by: CGFloat(pixels) / 1024)
        transform.concat()
        NSColor(calibratedRed: 0.055, green: 0.13, blue: 0.10, alpha: 1).setFill()
        NSBezierPath(roundedRect: NSRect(x: 72, y: 72, width: 880, height: 880), xRadius: 196, yRadius: 196).fill()
        let color = NSColor(calibratedRed: 0.706, green: 0.839, blue: 0.631, alpha: 1)
        color.setStroke()
        let rear = NSBezierPath()
        rear.move(to: NSPoint(x: 420, y: 810)); rear.line(to: NSPoint(x: 810, y: 810))
        rear.line(to: NSPoint(x: 810, y: 400)); rear.lineWidth = 62; rear.lineJoinStyle = .round; rear.stroke()
        let screen = NSBezierPath(roundedRect: NSRect(x: 210, y: 340, width: 460, height: 350), xRadius: 28, yRadius: 28)
        screen.lineWidth = 62; screen.stroke()
        let foot = NSBezierPath(); foot.move(to: NSPoint(x: 440, y: 340)); foot.line(to: NSPoint(x: 440, y: 220))
        foot.move(to: NSPoint(x: 280, y: 220)); foot.line(to: NSPoint(x: 600, y: 220)); foot.lineWidth = 62; foot.stroke()
        let moon = NSBezierPath()
        moon.appendArc(withCenter: NSPoint(x: 440, y: 520), radius: 102, startAngle: 85, endAngle: 290)
        moon.curve(to: NSPoint(x: 449, y: 622), controlPoint1: NSPoint(x: 400, y: 510), controlPoint2: NSPoint(x: 395, y: 578))
        moon.close(); color.setFill(); moon.fill()
        NSGraphicsContext.restoreGraphicsState()
        guard let data = rep.representation(using: .png, properties: [:]) else { fatalError("PNG export failed") }
        let suffix = scale == 2 ? "@2x" : ""
        try data.write(to: directory.appendingPathComponent("icon_\(points)x\(points)\(suffix).png"))
    }
}
