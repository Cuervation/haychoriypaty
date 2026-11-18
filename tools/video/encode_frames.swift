// Encodes numbered JPG/PNG frames into an H.264 MP4 with AVFoundation (no external tools).
// Usage: swift encode_frames.swift <frames_dir> <output.mp4> [fps]
import AVFoundation
import AppKit

let args = CommandLine.arguments
guard args.count >= 3 else { print("usage: encode_frames <frames_dir> <output.mp4> [fps]"); exit(2) }
let dir = args[1], output = args[2]
let fps = args.count > 3 ? Int32(args[3]) ?? 20 : 20

let files = (try FileManager.default.contentsOfDirectory(atPath: dir))
    .filter { $0.hasSuffix(".jpg") || $0.hasSuffix(".png") }.sorted()
guard let first = files.first, let firstImage = NSImage(contentsOfFile: dir + "/" + first),
      let firstRep = firstImage.representations.first else { print("no frames"); exit(1) }
let width = firstRep.pixelsWide, height = firstRep.pixelsHigh

try? FileManager.default.removeItem(atPath: output)
let writer = try AVAssetWriter(outputURL: URL(fileURLWithPath: output), fileType: .mp4)
let input = AVAssetWriterInput(mediaType: .video, outputSettings: [
    AVVideoCodecKey: AVVideoCodecType.h264, AVVideoWidthKey: width, AVVideoHeightKey: height,
    AVVideoCompressionPropertiesKey: [AVVideoAverageBitRateKey: 6_000_000]])
let adaptor = AVAssetWriterInputPixelBufferAdaptor(assetWriterInput: input, sourcePixelBufferAttributes: [
    kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32ARGB,
    kCVPixelBufferWidthKey as String: width, kCVPixelBufferHeightKey as String: height])
writer.add(input)
writer.startWriting()
writer.startSession(atSourceTime: .zero)

func buffer(_ path: String) -> CVPixelBuffer? {
    guard let image = NSImage(contentsOfFile: path),
          let cg = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else { return nil }
    var pixels: CVPixelBuffer?
    CVPixelBufferCreate(nil, width, height, kCVPixelFormatType_32ARGB, nil, &pixels)
    guard let buf = pixels else { return nil }
    CVPixelBufferLockBaseAddress(buf, [])
    let ctx = CGContext(data: CVPixelBufferGetBaseAddress(buf), width: width, height: height, bitsPerComponent: 8,
                        bytesPerRow: CVPixelBufferGetBytesPerRow(buf), space: CGColorSpaceCreateDeviceRGB(),
                        bitmapInfo: CGImageAlphaInfo.noneSkipFirst.rawValue)
    ctx?.draw(cg, in: CGRect(x: 0, y: 0, width: width, height: height))
    CVPixelBufferUnlockBaseAddress(buf, [])
    return buf
}

for (index, file) in files.enumerated() {
    while !input.isReadyForMoreMediaData { Thread.sleep(forTimeInterval: 0.005) }
    if let buf = buffer(dir + "/" + file) {
        adaptor.append(buf, withPresentationTime: CMTime(value: Int64(index), timescale: fps))
    }
}
input.markAsFinished()
let done = DispatchSemaphore(value: 0)
writer.finishWriting { done.signal() }
done.wait()
print("wrote \(output) frames=\(files.count) size=\(width)x\(height) fps=\(fps) status=\(writer.status.rawValue)")
