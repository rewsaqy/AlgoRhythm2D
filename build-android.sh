#!/usr/bin/env bash
# build-android.sh — Skrip otomatisasi build & instalasi AlgoRhythm2D Android
set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNITY_BIN="/home/zxwyvern/Unity/Hub/Editor/6000.3.8f1/Editor/Unity"
OUTPUT_APK="$PROJECT_DIR/Builds/Android/AlgoRhythm2D.apk"
LOG_FILE="$PROJECT_DIR/build.log"

echo "================================================="
echo "   AlgoRhythm2D Android Release Builder"
echo "================================================="

# Periksa apakah flag hanya untuk install
if [ "$1" == "--install-only" ]; then
    echo "Memasang APK yang sudah ada ke HP via ADB..."
    adb install -r -d "$OUTPUT_APK"
    adb shell monkey -p com.DefaultCompany.AlgoRhythm2D -c android.intent.category.LAUNCHER 1
    echo "Selesai!"
    exit 0
fi

# Cek apakah Unity Editor sedang berjalan dengan project ini
if pgrep -f "projectPath.*AlgoRhythm2D" > /dev/null 2>&1 || pgrep -f "Unity.*AlgoRhythm2D" > /dev/null 2>&1; then
    echo "[INFO] Unity Editor sedang terbuka di background."
    echo "Pilihan Build:"
    echo " 1. Di dalam Unity Editor: Klik menu 'AlgoRhythm > Build > Android APK (Release)'"
    echo " 2. Atau tutup Unity Editor terlebih dahulu jika ingin build via terminal ini."
    echo ""
    read -p "Apakah Anda ingin langsung memasang APK terakhir ke HP? (y/n): " confirm
    if [[ "$confirm" =~ ^[Yy]$ ]]; then
        adb install -r -d "$OUTPUT_APK"
        adb shell monkey -p com.DefaultCompany.AlgoRhythm2D -c android.intent.category.LAUNCHER 1
        echo "Selesai!"
    fi
    exit 0
fi

echo "[1/3] Memulai Unity Build Pipeline (IL2CPP ARM64 Release)..."
"$UNITY_BIN" -batchmode -quit \
  -projectPath "$PROJECT_DIR" \
  -executeMethod AlgoRhythm.Editor.AndroidBuildScript.BuildReleaseApk \
  -logFile "$LOG_FILE"

if [ -f "$OUTPUT_APK" ]; then
    echo "[2/3] Build SUKSES! File: $OUTPUT_APK"
    ls -lh "$OUTPUT_APK"
else
    echo "[ERROR] File APK tidak ditemukan. Cek log di: $LOG_FILE"
    exit 1
fi

if [ "$1" == "--install" ] || [ "$1" == "-i" ]; then
    echo "[3/3] Memasang ke perangkat via ADB..."
    if adb get-state 1>/dev/null 2>&1; then
        adb install -r -d "$OUTPUT_APK"
        adb shell monkey -p com.DefaultCompany.AlgoRhythm2D -c android.intent.category.LAUNCHER 1
        echo "Aplikasi berhasil dipasang dan diluncurkan!"
    else
        echo "[Peringatan] Perangkat tidak terdeteksi via ADB."
    fi
fi
