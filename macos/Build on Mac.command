#!/bin/bash
set -euo pipefail
TASK_ROOT="$(cd "$(dirname "$0")" && pwd)"
if /bin/bash "$TASK_ROOT/scripts/build-macos.sh"; then
    echo '完成。 / Done.'
else
    echo '构建失败，请保留上面的报错和 dist 下的日志。 / Build failed; keep the error and dist logs.'
fi
read -r -p '按回车关闭 / Press Return to close… ' task_reply
