import argparse
import base64
import io
import json
import multiprocessing
import os
from pathlib import Path
from re import L
import subprocess
import time
import traceback
from typing import Any, Iterable, Optional, Sequence, Union, cast
import zlib
import cv2
from cv2 import CAP_PROP_FRAME_HEIGHT
from cv2 import VideoCapture
from cv2 import CAP_PROP_POS_FRAMES
from cv2 import Mat
import numpy as np
import compressor


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

    def write(self, path: os.PathLike):
        path = Path(path)
        p = multiprocessing.Process(target=self._write, args=(path,))
        p.start()

    def _write(self, path: os.PathLike):
        print("start write")
        fourcc = cv2.VideoWriter.fourcc(*'XVID')
        out = cv2.VideoWriter(str(path), fourcc, self.fps, self.output_size)
        for f in self.frames:
            out.write(f.frame)

        out.release()
        print("start compress")
        output_path = compressor.compress(path)
        print("complete compress")
        self._onComplete(Path(path), output_path)

    def _onComplete(self, path: Path, output_path: Path):
        print(f"Saved to {output_path}")
        print(f"Removing original {path}")
        # os.remove(path)

        print(f"json output")
        with open(output_path.with_suffix(".json"), "w") as f:
            json.dump(self.frames, f, default=serializer, indent=2)

        print("done write")


def serializer(obj):
    if isinstance(obj, FrameInfo):
        return {"frame_number": obj.frame_number, "regions": obj.regions}

    if isinstance(obj, BoundingBoxWithContour):
        return {"bounding_box": obj.bounding_box}

    if isinstance(obj, np.ndarray):
        return obj.tolist()


RegionsList = list[tuple[tuple[float, float, float, float], np.ndarray]]


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

    def try_add_frame(self, frame: Mat, regions: RegionsList):
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

    @staticmethod
    def start(queue: multiprocessing.Queue):
        CLIP_DURATION = 10
        processed_first_message = False
        frame_count = 0
        output_size: tuple[int, int] = None  # type: ignore
        fps: int = None  # type: ignore
        clips: ClipManager = None  # type: ignore

        try:
            while True:
                try:
                    data: Union[tuple[tuple[int, int], int], tuple[Mat,
                                                                   RegionsList], bool] = queue.get(block=True, timeout=3)

                    if isinstance(data, bool):
                        break

                    if not processed_first_message:
                        processed_first_message = True
                        output_size, fps = cast(
                            tuple[tuple[int, int], int], data)
                        capture_area = output_size[0] * output_size[1]
                        clips = ClipManager(output_size, fps, CLIP_DURATION)
                    else:
                        data = cast(tuple[Mat, RegionsList], data)
                        frame_count += 1
                        (frame, matches) = data

                        if len(matches) != 0 or clips._match_started:
                            clips.try_start()
                            clips.try_add_frame(frame, matches)
                            if clips.should_complete():
                                clip = clips.complete()
                                if clip is None:
                                    raise Exception(
                                        "Expected to recieve a clip")
                                clip.write(f"{frame_count}.avi")
                except Exception:
                    (_, clip) = clips.try_complete()
                    if clip is not None:
                        clip.write(f"{frame_count}_clip.avi")
                    print(traceback.format_exc())
        except Exception:
            print(traceback.format_exc())


def start_processing(process_queue: multiprocessing.Queue):
    p = multiprocessing.Process(
        target=ClipManager.start, args=(process_queue,))
    p.start()


if __name__ == "__main__":
    pass
