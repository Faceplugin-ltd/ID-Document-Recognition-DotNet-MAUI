#!/bin/bash
# Re-sign nested dcrcore.framework inside embedded docsdk.framework with the app identity.
# Invoked from the MAUI iOS project before CodesignAppBundle.
set -euo pipefail

APP_BUNDLE="${1:?app bundle path required}"
SIGN_IDENTITY="${2:?codesign identity required}"

DOCSDK_FW="${APP_BUNDLE}/Frameworks/docsdk.framework"
NESTED=""
for candidate in dcrcore.framework DocumentReaderCore.framework; do
  if [ -d "${DOCSDK_FW}/Frameworks/${candidate}" ]; then
    NESTED="${DOCSDK_FW}/Frameworks/${candidate}"
    break
  fi
done

if [ -z "${NESTED}" ]; then
  exit 0
fi

if [ -z "${SIGN_IDENTITY}" ] || [ "${SIGN_IDENTITY}" = "-" ]; then
  echo "warning: codesign identity unset; skipping nested dcrcore codesign" >&2
  exit 0
fi

echo "Signing nested $(basename "${NESTED}")"
/usr/bin/codesign --remove-signature "${NESTED}" 2>/dev/null || true
/usr/bin/codesign --force --sign "${SIGN_IDENTITY}" \
  --preserve-metadata=identifier,entitlements,flags \
  --timestamp=none \
  "${NESTED}"

echo "Re-signing docsdk.framework"
/usr/bin/codesign --remove-signature "${DOCSDK_FW}" 2>/dev/null || true
/usr/bin/codesign --force --sign "${SIGN_IDENTITY}" \
  --preserve-metadata=identifier,entitlements,flags \
  --timestamp=none \
  "${DOCSDK_FW}"
