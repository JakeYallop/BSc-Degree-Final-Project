import argparse
import multiprocessing
import os
from pathlib import Path

import cv2

from clip_manager import ClipManager
from capture import capture


if __name__ == "__main__":
    print("Ensure API_BASE_URL is set as an environment variable to make sure that clips are exported to the API. API_BASE_URL should be the URL to the Web project (the web API).")

    import sys
    parser = argparse.ArgumentParser(description="camera detection")
    parser.add_argument("-d", "--device", type=int,
                        help="camera device")
    parser.add_argument("-f", "--file", type=str,
                        help="Path to video file, useful for testing purposes")
    args = parser.parse_args(sys.argv[1:])

    filepath = args.file
    device = args.device

    if filepath is None and device is None:
        print("Error: file path or device must be specified")
        exit(1)
    elif filepath is not None and device is not None:
        print("Error: Only one of filepath or device can be specified")
        exit(1)

    deviceOrPath = ""
    if filepath is not None:
        deviceOrPath = str(Path(filepath).resolve())
    if device is not None:
        deviceOrPath = device

    video = cv2.VideoCapture(
        deviceOrPath)

    if not video.isOpened():
        print("Could not open video device or file.")
        exit(1)

    queue = multiprocessing.Queue()

    out_dir = Path("./clips")
    if not out_dir.exists():
        os.mkdir(out_dir)

    ClipManager.start_processing(queue, out_dir)
    capture(video, queue)
    video = cv2.VideoCapture(
        deviceOrPath)
