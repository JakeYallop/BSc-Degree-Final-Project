import argparse
import multiprocessing
import os
from pathlib import Path
import time
import traceback
from typing import cast
from bs_morphology_solver import morphology_solver
import cv2
from cv2 import CAP_PROP_FRAME_HEIGHT
from cv2 import VideoCapture
from cv2 import CAP_PROP_POS_FRAMES
from cv2 import Mat
import clip_manager


def display(window_name, frame, show_frame=True):
    if (show_frame):
        cv2.imshow(window_name, frame)


def get_output_size(capture: VideoCapture):  # type: ignore
    return (int(capture.get(cv2.CAP_PROP_FRAME_WIDTH)),
            int(capture.get(CAP_PROP_FRAME_HEIGHT)))


def get_predicted_fps(capture: VideoCapture):  # type: ignore
    file_fps = capture.get(cv2.CAP_PROP_FPS)
    if file_fps != 0:
        return file_fps

    NUMBER_OF_FRAMES = 360
    start = time.time()
    for i in range(0, NUMBER_OF_FRAMES):
        capture.read()
    seconds = time.time() - start
    capture.set(CAP_PROP_POS_FRAMES, 0)
    return NUMBER_OF_FRAMES / seconds


def is_key(input, key):
    return input & 0xFF == ord(key)


RUN_AT_FRAMERATE = False
solver = morphology_solver()


def capture(capture: VideoCapture, queue: multiprocessing.Queue):  # type: ignore
    MIN_RELATIVE_CONTOUR_AREA = 1.0 / 100
    MIN_FRAME_RESET_CONTOUR_AREA = 0.1 / 100

    def is_valid_contour(contour, bounding_rect, capture_area):
        (x, y, w, h) = bounding_rect
        area = cv2.contourArea(contour)
        # relative_area = (area / capture_area)
        relative_area = (w * h) / capture_area
        if relative_area < MIN_RELATIVE_CONTOUR_AREA:
            return False
        return True
        # aspect = w / h

    output_size = get_output_size(capture)
    fps = get_predicted_fps(capture)

    width, height = output_size
    max_width = min(1280, width)
    aspect = width/height
    max_height = max_width//aspect
    processing_size = (int(max_width), int(max_height))

    capture_area = processing_size[0] * processing_size[1]
    queue.put((processing_size, fps))

    try:
        frame_count = 0
        frames_since_last_reset = 0
        fps_ms = int(1000//fps)
        while True:
            frame_start = time.time()
            current_frame: Mat
            read_frame, raw_frame = capture.read()
            if read_frame == True:
                current_frame = cv2.resize(
                    raw_frame.copy(), (processing_size[0], processing_size[1]))
                frame_count += 1

                # show the original video frame
                display("original", current_frame, False)

                roi_x = 0
                roi_y = 0
                region_of_interest = current_frame[roi_y:, roi_x:]

                (contours, _) = solver.solve(region_of_interest, debug=False)
                matched_contours = []
                has_large_contours = False
                for contour in contours:
                    bounding_rect = cv2.boundingRect(contour)
                    area = cv2.contourArea(contour)
                    if not is_valid_contour(contour, bounding_rect, capture_area):
                        continue

                    has_large_contours = True

                    (x, y, w, h) = cast(
                        tuple[int, int, int, int], bounding_rect)

                    relative_area = (area / capture_area) * 100

                    # re-adjust the coordinates so they appear in the correct
                    # place on the original frame
                    adjusted_y = y + roi_y
                    adjusted_x = x + roi_x

                    matched_contours.append(
                        ((adjusted_x, adjusted_y, w, h), contour, relative_area))

                if has_large_contours:
                    frames_since_last_reset = 0
                else:
                    if frames_since_last_reset >= 600:
                        frames_since_last_reset = 0
                        # reference_frame = current_frame
                        # print("Updated reference frame")
                    else:
                        frames_since_last_reset += 1

                contours = []
                if len(matched_contours) != 0:
                    contours = [(box, contour)
                                for (box, contour, _) in matched_contours]

                queue.put((current_frame.copy(), contours))

                for (box, contour, area) in matched_contours:
                    (x, y, w, h) = box
                    cv2.putText(current_frame, f"S: {area:.6}", (x - 20, y - 20),
                                cv2.FONT_HERSHEY_SIMPLEX, 1.1, (255, 255, 255), 4, 2)
                    cv2.rectangle(current_frame, (x, y),
                                  (x + w, y + h), (0, 255, 0), 1)
                    # cv2.polylines(current_frame, contour,
                    #               True, (0, 0, 255), 2)

                display(
                    "contours", current_frame)

                frame_end = time.time()
                duration_s = frame_end - frame_start
                # duration = int(duration_s * 1000)
                wait = fps - duration_s
                key = None
                if wait > 0 and RUN_AT_FRAMERATE:
                    key = cv2.waitKey(int(wait * 1000.0))
                else:
                    key = cv2.waitKey(1)
                if is_key(key, "q"):
                    # exit the application
                    break
                elif is_key(key, "p"):
                    cv2.waitKey(-1)
                # elif frame_count == 820:
                #     cv2.waitKey(-1)
            else:
                break
    except Exception:
        print(traceback.format_exc())

    print("ending subprocesses")
    queue.put(True)
    capture.release()
    cv2.destroyAllWindows()


# if __name__ == "__main__":
#     print("Ensure API_BASE_URL is set as an environment variable to make sure that clips are exported to the API. API_BASE_URL should be the URL to the Web project (the web API).")

#     import sys
#     parser = argparse.ArgumentParser(description="camera detection")
#     parser.add_argument("-d", "--device", type=int,
#                         help="camera device")
#     parser.add_argument("-f", "--file", type=str,
#                         help="Path to video file, useful for testing purposes")
#     args = parser.parse_args(sys.argv[1:])

#     filepath = args.filepath
#     device = args.device

#     if filepath is None and device is None:
#         print("Error: file path or device must be specified")
#     elif filepath is not None and device is not None:
#         print("Error: Only one of filepath or device can be specified")

#     deviceOrPath = ""
#     if filepath is not None:
#         deviceOrPath = str(Path(filepath).resolve())
#     if device is not None:
#         deviceOrPath = device

#     video = cv2.VideoCapture(
#         deviceOrPath)

#     if not video.isOpened():
#         print("Could not open video device or file.")
#         exit(1)

#     queue = multiprocessing.Queue()

#     out_dir = Path("./clips")
#     if not out_dir.exists():
#         os.mkdir(out_dir)

#     clip_manager.start_processing(queue, out_dir)
#     capture(video, queue)
#     video = cv2.VideoCapture(
#         deviceOrPath)
