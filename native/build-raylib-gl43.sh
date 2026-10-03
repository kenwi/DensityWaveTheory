#!/usr/bin/env bash
# Build libraylib.so with OpenGL 4.3 (compute shaders + SSBO) and install
# into the .NET RID-specific native folder so Raylib-cs loads it.
#
# Prefers CMake when available; falls back to raylib's Makefile.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BUILD_DIR="${ROOT}/native/raylib-build"
# Raylib-cs probes runtimes/<rid>/native/ before the app directory.
DEFAULT_OUT="${ROOT}/bin/Debug/net10.0/runtimes/linux-x64/native"
OUT_DIR_ARG="${1:-${DEFAULT_OUT}}"
if [[ "${OUT_DIR_ARG}" = /* ]]; then
  OUT_DIR="${OUT_DIR_ARG}"
else
  OUT_DIR="${ROOT}/${OUT_DIR_ARG#./}"
fi

# Match Raylib-cs 8.1 (official raylib 6.0).
RAYLIB_REF="${RAYLIB_REF:-6.0}"

mkdir -p "${BUILD_DIR}" "${OUT_DIR}"
cd "${BUILD_DIR}"

if [[ ! -d raylib ]]; then
  git clone --depth 1 --branch "${RAYLIB_REF}" https://github.com/raysan5/raylib.git
fi

SO_PATH=""

if command -v cmake >/dev/null 2>&1; then
  cmake -S raylib -B build \
    -DCMAKE_BUILD_TYPE=Release \
    -DBUILD_EXAMPLES=OFF \
    -DBUILD_SHARED_LIBS=ON \
    -DGRAPHICS=GRAPHICS_API_OPENGL_43
  cmake --build build -j"$(nproc)"
  SO_PATH="$(find build -name 'libraylib.so' | head -n1)"
else
  echo "cmake not found; building with raylib Makefile..."
  make -C raylib/src clean || true
  make -C raylib/src -j"$(nproc)" \
    PLATFORM=PLATFORM_DESKTOP \
    GRAPHICS=GRAPHICS_API_OPENGL_43 \
    RAYLIB_LIBTYPE=SHARED
  SO_PATH="$(find raylib -name 'libraylib.so' | head -n1)"
fi

if [[ -z "${SO_PATH}" || ! -f "${SO_PATH}" ]]; then
  echo "Could not find built libraylib.so" >&2
  exit 1
fi

cp -f "${SO_PATH}"* "${OUT_DIR}/"

# Keep a redistributable copy under native/
mkdir -p "${ROOT}/native/raylib-gl43"
cp -f "${SO_PATH}"* "${ROOT}/native/raylib-gl43/"

# Also place beside the managed binary (secondary probe path).
APP_OUT="$(dirname "$(dirname "$(dirname "${OUT_DIR}")")")"
if [[ -d "${APP_OUT}" ]]; then
  cp -f "${SO_PATH}"* "${APP_OUT}/" || true
fi

echo "Installed OpenGL 4.3 libraylib into ${OUT_DIR}"
echo "Run: dotnet run --project ${ROOT}"
