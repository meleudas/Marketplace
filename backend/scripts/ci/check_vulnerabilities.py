import json
import subprocess
import sys


def main() -> int:
    cmd = [
        "dotnet",
        "list",
        "backend/Marketplace.slnx",
        "package",
        "--vulnerable",
        "--include-transitive",
        "--format",
        "json",
    ]
    result = subprocess.run(cmd, capture_output=True, text=True, check=False)
    if result.returncode != 0:
        print(result.stdout)
        print(result.stderr, file=sys.stderr)
        return result.returncode

    try:
        data = json.loads(result.stdout or "{}")
    except json.JSONDecodeError:
        print(result.stdout)
        return 1

    severities = {"critical", "high"}
    findings = []

    projects = data.get("projects", [])
    for project in projects:
        frameworks = project.get("frameworks", [])
        for framework in frameworks:
            for package in framework.get("topLevelPackages", []) + framework.get("transitivePackages", []):
                for advisory in package.get("vulnerabilities", []):
                    severity = str(advisory.get("severity", "")).lower()
                    if severity in severities:
                        findings.append(
                            {
                                "project": project.get("path", ""),
                                "package": package.get("id", ""),
                                "severity": severity,
                                "advisoryUrl": advisory.get("advisoryurl", ""),
                            }
                        )

    if findings:
        print("High/Critical vulnerabilities found:")
        for item in findings:
            print(f"- {item['severity'].upper()} {item['package']} ({item['project']}) {item['advisoryUrl']}")
        return 2

    print("No High/Critical vulnerabilities found.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
