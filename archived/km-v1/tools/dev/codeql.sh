#!/usr/bin/env bash

set -e

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
CODEQL_DB=${REPO_DIR}/.codeql/db
CODEQL_REPORT=${REPO_DIR}/.codeql/results.sarif

cd $REPO_DIR

echo -e "\033[1;32m- Repository:\033[0m ${REPO_DIR}"
echo -e "\033[1;32m- Report    :\033[0m ${CODEQL_REPORT}.sarif\n"
read -p "Press Enter to DELETE the existing CodeQL results and RUN A NEW analysis."

mkdir -p ${REPO_DIR}/.codeql
rm -f ${CODEQL_REPORT}

echo -e "\033[1;32m\n### Install CodeQL C# queries ###\033[0m"
codeql pack download "codeql/csharp-queries"

echo -e "\033[1;32m\n### Perform CodeQL Analysis ###\033[0m"
rm -fR ${CODEQL_DB}
codeql database create ${CODEQL_DB} --source-root=${REPO_DIR} --language=csharp --build-mode=autobuild
codeql database print-baseline ${CODEQL_DB}

echo -e "\033[1;32m\n### Export CodeQL results ###\033[0m"
codeql database analyze ${CODEQL_DB} --format=sarif-latest --output=${CODEQL_REPORT}

echo -e "\033[1;32m\n### Done ###\033[0m"
echo -e "\033[1;32m- Report:\033[0m ${CODEQL_REPORT}"
