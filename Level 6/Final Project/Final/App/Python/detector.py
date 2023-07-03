import argparse
from re import L
import time
import traceback
from typing import Iterable, Optional, Sequence, Union, cast
import cv2
from cv2 import CAP_PROP_FRAME_HEIGHT
from cv2 import VideoCapture
from cv2 import CAP_PROP_POS_FRAMES
from cv2 import Mat
from isort import file
import numpy as np


class BoundingBoxWithContour():
    def __init__(self, box: tuple[float, float, float, float], contour: np.ndarray) -> None:
        self.bounding_box = box
        self.contour = contour


class FrameInfo():
    def __init__(self, frame: Mat, frame_number: int, regions: list[BoundingBoxWithContour]) -> None:
        self.frame = frame
        self.frame_number = frame_number
        self.regions = regions


class Clip():
    def __init__(self, fps: float, output_size: tuple[int, int]) -> None:
        self.frames: list[FrameInfo] = []
        self.fps = fps
        self.output_size = output_size

    def append(self, frame_info: FrameInfo) -> None:
        self.frames.append(frame_info)

    def write(self, path: str):
        fourcc = cv2.VideoWriter.fourcc(*'XVID')
        out = cv2.VideoWriter(path, fourcc, self.fps, self.output_size)
        for f in self.frames:
            out.write(f.frame)

        out.release()


class ClipManager():
    def __init__(self, output_size: tuple[int, int], fps: float, clip_duration) -> None:
        self._match_started = False
        self.current_clip: Optional[Clip]
        self.fps: float = fps
        self.output_size = output_size
        self._clip_duration = clip_duration
        self._clip_count = 0

    def try_start(self):
        if self._match_started:
            return False
        self._match_started = True
        self.current_clip = Clip(self.fps, self.output_size)
        self._clip_count = self.fps * self._clip_duration

        return True

    def try_add_frame(self, frame: Mat, regions: list[tuple[tuple[float, float, float, float], np.ndarray]]):
        if not self._match_started or self.current_clip is None:
            return False

        self.current_clip.append(FrameInfo(frame, len(
            self.current_clip.frames), [BoundingBoxWithContour(b, c) for (b, c) in regions]))

        return True

    def should_complete(self):
        if not self._match_started or self.current_clip is None:
            return

        return self._clip_count == len(self.current_clip.frames)

    def complete(self):
        (success, clip) = self.try_complete()
        assert (clip is not None)
        return clip

    def try_complete(self):
        if not self._match_started:
            return (False, None)

        self._clip_count = 0
        self._match_started = False
        clip = self.current_clip
        self.current_clip = None
        return (True, cast(Clip, clip))


def display(window_name, frame, show_frame=True):
    if (show_frame):
        cv2.imshow(window_name, frame)


def convert_to_greyscale(frame, show_frame=True):
    frame_processed = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    display("greyscale", frame_processed, show_frame)
    return frame_processed


def blur_frame_gaussian(frame, show_frame=True):
    kernel_size = (25, 25)
    processed = cv2.GaussianBlur(frame, kernel_size, 0)
    display("gaussian_blur", processed, show_frame)
    return processed


def preprocess(frame, show_frame=True):
    greyscale = convert_to_greyscale(frame, show_frame)
    blurred = blur_frame_gaussian(greyscale, show_frame)
    return blurred


def background_subtraction(initial_frame, frame, show_frame=True):
    processed = cv2.absdiff(initial_frame, frame)
    display("background subtraction", processed, show_frame)
    return processed


mask_thresh = 5


def apply_mask(frame, show_frame=True):
    mask = cv2.inRange(frame, np.asarray([mask_thresh]), np.asarray([255]))
    masked_frame = cv2.bitwise_and(frame, frame.copy(), mask=mask)
    display("Masked", frame, show_frame)
    return masked_frame


def normalize_frame(frame, show_frame=True):
    normalized = cv2.normalize(frame, np.zeros(
        frame.shape), 0, 255, cv2.NORM_MINMAX)  # type: ignore
    display("Normalized", normalized, show_frame)
    return normalized


min_thresh = 40


def apply_thresholding(frame, show_frame=True):
    thresh_frame = cv2.threshold(frame, min_thresh, 255, cv2.THRESH_BINARY)[1]
    return thresh_frame


def fill_and_smooth_internal_holes(frame, show_frame=True):
    kernel = np.ones((7, 7), np.uint8)
    morph_frame = cv2.morphologyEx(frame, cv2.MORPH_CLOSE, kernel)
    display("Morph", morph_frame, show_frame)
    return morph_frame


def process(initial_frame, frame, show_frame=True):
    background_subtracted_frame = background_subtraction(
        initial_frame, frame, show_frame)
    masked = apply_mask(background_subtracted_frame, show_frame)
    normalized = normalize_frame(masked, show_frame)
    binary_thresholded_frame = apply_thresholding(normalized, show_frame)
    smoothed = fill_and_smooth_internal_holes(
        binary_thresholded_frame, show_frame)
    return smoothed


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


def capture(capture: VideoCapture):  # type: ignore
    MIN_RELATIVE_CONTOUR_AREA = 0.5 / 100
    CLIP_DURATION = 20

    def is_valid_contour(contour, bounding_rect, capture_area):
        (x, y, w, h) = bounding_rect
        area = cv2.contourArea(contour)
        relative_area = (area / capture_area)
        if relative_area < MIN_RELATIVE_CONTOUR_AREA:
            return False
        return True
        # aspect = w / h
        # # cars are longer than they are tall
        # # we can use the aspect ratio to check if we are tracking a person or a vehicle
        # # When the vehicle turns up the junction, the bounding rect for contour is much taller than it is longer
        # return (aspect > 1.2 and aspect < 3) or (aspect < 1 and aspect > 0.4)

    output_size = get_output_size(video)
    fps = get_predicted_fps(video)
    capture_area = output_size[0] * output_size[1]

    try:
        initial_frame = None
        frames = []
        current_frame = 0
        clips = ClipManager(output_size, fps, CLIP_DURATION)
        fps_ms = int(1000//fps)
        while True:
            frame_start = time.time()
            frame_orig: Mat
            success, frame_orig = capture.read()
            frames.append(frame_orig)
            current_frame += 1
            adjusted_offset = max(int(current_frame), 0)
            adjusted_offset = min(len(frames) - 1, adjusted_offset)
            frame_orig = frames[adjusted_offset]
            if success == True:
                # show the original video frame
                display("original", frame_orig, False)

                width, height, _ = frame_orig.shape
                roi_x = 0
                roi_y = 0
                region_of_interest = frame_orig[roi_y:, roi_x:]

                # preprocess frame
                preprocessed_frame = preprocess(region_of_interest, False)
                if initial_frame is None:
                    initial_frame = preprocessed_frame

                processed_frame = process(
                    initial_frame, preprocessed_frame, False)
                (contours, _) = cv2.findContours(processed_frame,
                                                 cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
                matched_contours = []
                draw_frame = frame_orig
                for contour in contours:
                    bounding_rect = cv2.boundingRect(contour)
                    area = cv2.contourArea(contour)
                    if not is_valid_contour(contour, bounding_rect, capture_area):
                        continue

                    (x, y, w, h) = cast(
                        tuple[int, int, int, int], bounding_rect)

                    relative_area = (area / capture_area) * 100
                    matched_contours.append(
                        ((x, y, w, h), contour))

                    if draw_frame is None:
                        draw_frame = frame_orig[:, :]

                    cv2.putText(draw_frame, f"S: {relative_area:.6}", (x - 20, y - 20),
                                cv2.FONT_HERSHEY_SIMPLEX, 1.1, (255, 255, 255), 4, 2)
                    # re-adjust the coordinates so they appear in the correct
                    # place on the original frame
                    adjusted_y = y + roi_y
                    adjusted_x = x + roi_x
                    cv2.rectangle(draw_frame, (adjusted_x, adjusted_y),  # type: ignore
                                  (adjusted_x + w, adjusted_y + h), (0, 255, 0), 1)

                if len(matched_contours) != 0:
                    clips.try_start()
                    clips.try_add_frame(frame_orig, matched_contours)
                    if clips.should_complete():
                        clip = clips.complete()
                        if clip is None:
                            raise Exception("Expected to recieve a clip")
                        clip.write(f"{current_frame}.avi")

                display("contours", draw_frame)

                frame_end = time.time()
                duration = int((frame_end - frame_start) * 1000)
                wait = fps_ms - duration
                key = None
                if wait > 0:
                    key = cv2.waitKey(wait)
                else:
                    key = cv2.waitKey(1)
                if is_key(key, "q"):
                    # exit the application
                    break
                # Step forward and backward through frames, from the first frame up to the latest played frame.
                if is_key(key, "b"):
                    current_frame = max(current_frame - fps, 0)
                if is_key(key, "f"):
                    current_frame = min(current_frame + fps, len(frames) - 1)

                if (len(frames) > 2000):
                    frames.pop(0)
            else:
                (success, clip) = clips.try_complete()
                if clip is not None:
                    clip.write(f"{current_frame}_clip.avi")
                break
    except Exception:
        print(traceback.format_exc())

    video.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":

    import sys
    parser = argparse.ArgumentParser(description="camera detection")
    parser.add_argument("-d", "--device", type=int,
                        help="camera device")
    parser.add_argument("filepath", type=str,
                        help="file path")
    args = parser.parse_args(sys.argv[1:])

    filepath = args.filepath
    device = args.device

    if filepath is None and device is None:
        print("Error: file path or device must be specified")
    elif filepath is not None and device is not None:
        print("Error: Only one of filepath or device can be specified")

    deviceOrPath = ""
    if filepath is not None:
        deviceOrPath = filepath
    if device is not None:
        deviceOrPath = device

    video = cv2.VideoCapture(
        deviceOrPath)

    if not video.isOpened():
        print("Could not open video device or file.")
        exit(1)

    capture(video)
else:
    print(f"{__name__} Expected to be run as a module, and not imported into a script file")
