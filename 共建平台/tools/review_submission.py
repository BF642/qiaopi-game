#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""侨批图集 · 共建投稿审核工具（维护者本机运行）。

用法：
  python3 tools/review_submission.py --list                       列出图集仓库里的条目与提供者
  python3 tools/review_submission.py <投稿包目录>                  审核通过：收图片、写条目、标记提供者已通过
  python3 tools/review_submission.py <投稿包目录> --reject "原因"   记录未通过原因（游戏里显示为「未通过」）
  python3 tools/review_submission.py --rebuild                    从 gallery/entries/ 重建 gallery/index.json
  python3 tools/review_submission.py --check                      只校验索引与条目，不写任何文件

投稿包由游戏生成（存档目录/gallery/submissions/<编号>/），里面有 entry.json、投稿说明.txt 与图片。
审核通过后提交并推送本仓库，玩家的游戏端刷新即可看到新条目，并且该提供者编号被记为已通过。
"""
import argparse
import datetime
import json
import hashlib
import os
import re
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GALLERY = os.path.join(ROOT, "gallery")
INDEX_PATH = os.path.join(GALLERY, "index.json")
ENTRIES_DIR = os.path.join(GALLERY, "entries")
IMAGES_DIR = os.path.join(GALLERY, "images")
CATEGORIES = ["侨批信", "回批", "批封", "汇票与单据", "信局与器物", "老照片", "口述与文字", "其他"]
IMAGE_EXT = (".png", ".jpg", ".jpeg", ".webp")
ID_RE = re.compile(r"^[A-Za-z0-9._-]{1,64}$")


def today():
    return datetime.date.today().isoformat()


def load_index():
    if os.path.exists(INDEX_PATH):
        with open(INDEX_PATH, encoding="utf-8") as fh:
            index = json.load(fh)
    else:
        index = {"version": 1, "updated": today(), "indexUrl": "", "repo": "", "contributors": [], "notes": [], "entries": []}
    for key, default in (("contributors", []), ("notes", []), ("entries", [])):
        index.setdefault(key, default)
    return index


def save_index(index):
    index["updated"] = today()
    os.makedirs(GALLERY, exist_ok=True)
    with open(INDEX_PATH, "w", encoding="utf-8") as fh:
        json.dump(index, fh, ensure_ascii=False, indent=2)
        fh.write("\n")


def save_entry(entry):
    os.makedirs(ENTRIES_DIR, exist_ok=True)
    with open(os.path.join(ENTRIES_DIR, entry["id"] + ".json"), "w", encoding="utf-8") as fh:
        json.dump(entry, fh, ensure_ascii=False, indent=2)
        fh.write("\n")


def validate(entry):
    problems = []
    title = (entry.get("title") or "").strip()
    if not 2 <= len(title) <= 40:
        problems.append("题名需要 2 至 40 个字")
    if (entry.get("category") or "").strip() not in CATEGORIES:
        problems.append("类别不在允许列表里：" + str(entry.get("category")))
    if len((entry.get("description") or "").strip()) < 8:
        problems.append("说明至少 8 个字")
    if len((entry.get("source") or "").strip()) < 2:
        problems.append("缺少来源与授权说明")
    if not (entry.get("contributor") or "").strip():
        problems.append("缺少署名")
    return problems


def read_package(package):
    manifest = os.path.join(package, "entry.json")
    if not os.path.isfile(manifest):
        sys.exit("找不到投稿包清单：%s（应指向游戏生成的投稿包目录）" % manifest)
    with open(manifest, encoding="utf-8") as fh:
        data = json.load(fh)
    if not data.get("id"):
        sys.exit("投稿包清单缺少 id")
    images = [f for f in sorted(os.listdir(package)) if f.lower().endswith(IMAGE_EXT)]
    if not images:
        sys.exit("投稿包里没有图片（支持 " + "、".join(IMAGE_EXT) + "）")
    if len(images) > 8:
        sys.exit("一次投稿最多 8 张图片，实际 %d 张" % len(images))
    if "rightsConfirmed" in data and not data["rightsConfirmed"]:
        sys.exit("投稿包尚未确认公开授权，请先向投稿者核实并请其重新生成投稿包")
    listed = data.get("files") or []
    hashes = data.get("hashes") or []
    if listed and (set(listed) != set(images) or len(listed) != len(hashes)):
        sys.exit("图片清单与投稿包不一致，请重新递交完整投稿包")
    for name, expected in zip(listed, hashes):
        if os.path.basename(name) != name:
            sys.exit("投稿清单含无效文件名")
        with open(os.path.join(package, name), "rb") as fh:
            actual = hashlib.sha256(fh.read()).hexdigest()
        if actual != expected:
            sys.exit("图片校验不符：" + name + "，请核对文件是否被改动")
    return data, images


def approve(package, index, keep_id=True):
    data, images = read_package(package)
    problems = validate(data)
    if problems:
        sys.exit("投稿不合规，先退回请投稿人修改：\n  - " + "\n  - ".join(problems))

    entry_id = data["id"] if keep_id else re.sub(r"[^A-Za-z0-9._-]", "-", data["title"])
    if not ID_RE.match(entry_id):
        sys.exit("条目 id 只能包含字母、数字、点、下划线与连字符：" + entry_id)
    target_dir = os.path.join(IMAGES_DIR, entry_id)
    os.makedirs(target_dir, exist_ok=True)
    for name in images:
        shutil.copyfile(os.path.join(package, name), os.path.join(target_dir, name))

    entry = {
        "id": entry_id,
        "title": data["title"].strip(),
        "category": data["category"].strip(),
        "year": (data.get("year") or "").strip(),
        "place": (data.get("place") or "").strip(),
        "contributor": data["contributor"].strip(),
        "contributorId": (data.get("contributorId") or "").strip(),
        "description": data["description"].strip(),
        "source": data["source"].strip(),
        "image": "gallery/images/%s/%s" % (entry_id, images[0]),
        "imageUrl": "",
        "submittedAt": data.get("createdAt") or today(),
        "sample": "",
        "visible": True,
    }
    entry["images"] = ["gallery/images/%s/%s" % (entry_id, name) for name in images]
    save_entry(entry)

    index["entries"] = [e for e in index["entries"] if e.get("id") != entry_id]
    index["entries"].append(entry)
    index["entries"].sort(key=lambda e: e.get("submittedAt") or "")
    contributor_id = entry["contributorId"]
    if contributor_id and contributor_id not in index["contributors"]:
        index["contributors"].append(contributor_id)
    index["notes"] = [n for n in index["notes"] if n.get("submissionId") != data["id"]]
    save_index(index)
    print("已收入图集：%s（%s，%d 张图片）" % (entry["title"], entry_id, len(images)))
    if contributor_id:
        print("提供者编号 %s 已记为共建者，审核结论按投稿编号独立记录。" % contributor_id)
    print("接下来：git add -A && git commit -m \"收入投稿 %s\" && git push" % entry_id)


def reject(package, index, reason):
    data, _ = read_package(package)
    contributor_id = (data.get("contributorId") or "").strip()
    if not contributor_id:
        sys.exit("投稿包缺少 contributorId，无法记录审核结论")
    index["notes"] = [n for n in index["notes"] if n.get("submissionId") != data["id"]]
    index["notes"].append({"contributorId": contributor_id, "submissionId": data["id"], "status": "rejected", "note": reason, "date": today()})
    save_index(index)
    print("已记录未通过：%s（%s）\n原因：%s" % (data.get("title"), contributor_id, reason))
    print("接下来：commit + push，玩家刷新后会看到「未通过」与这条原因。")


def rebuild(index):
    entries = []
    if os.path.isdir(ENTRIES_DIR):
        for name in sorted(os.listdir(ENTRIES_DIR)):
            if not name.endswith(".json"):
                continue
            with open(os.path.join(ENTRIES_DIR, name), encoding="utf-8") as fh:
                entries.append(json.load(fh))
    index["entries"] = entries
    seen = []
    for entry in entries:
        cid = (entry.get("contributorId") or "").strip()
        if cid and cid not in seen:
            seen.append(cid)
    for cid in seen:
        if cid not in index["contributors"]:
            index["contributors"].append(cid)
    save_index(index)
    print("索引已重建：%d 件藏品，%d 位已通过提供者" % (len(entries), len(index["contributors"])))


def check():
    index = load_index()
    problems = []
    seen = set()
    for entry in index["entries"]:
        if entry.get("id") in seen:
            problems.append("重复 id：" + str(entry.get("id")))
        seen.add(entry.get("id"))
        for problem in validate(entry):
            problems.append("%s：%s" % (entry.get("id"), problem))
        image = entry.get("image") or ""
        if image.startswith("gallery/") and not os.path.isfile(os.path.join(ROOT, image)):
            problems.append("%s：图片缺失 %s" % (entry.get("id"), image))
    if problems:
        print("索引有问题：")
        for problem in problems:
            print("  - " + problem)
        return 1
    print("索引正常：%d 件藏品，%d 位已通过提供者" % (len(index["entries"]), len(index["contributors"])))
    return 0


def main():
    parser = argparse.ArgumentParser(description="侨批图集 · 共建投稿审核")
    parser.add_argument("package", nargs="?", help="游戏生成的投稿包目录")
    parser.add_argument("--reject", metavar="原因", help="记录未通过原因")
    parser.add_argument("--rebuild", action="store_true", help="从 gallery/entries/ 重建索引")
    parser.add_argument("--check", action="store_true", help="只校验，不写文件")
    parser.add_argument("--list", action="store_true", help="列出当前索引内容")
    args = parser.parse_args()
    index = load_index()
    if args.check:
        return check()
    if args.list:
        print("藏品 %d 件，已通过提供者 %d 位，索引更新时间 %s" % (len(index["entries"]), len(index["contributors"]), index.get("updated")))
        for entry in index["entries"]:
            print("  - %s  [%s]  %s" % (entry.get("id"), entry.get("category"), entry.get("title")))
        for note in index["notes"]:
            print("  ! %s 未通过：%s" % (note.get("contributorId"), note.get("note")))
        return 0
    if args.rebuild:
        rebuild(index)
        return check()
    if not args.package:
        parser.error("请给出投稿包目录，或用 --list / --rebuild / --check")
    package = os.path.abspath(args.package)
    if not os.path.isdir(package):
        sys.exit("不是目录：" + package)
    if args.reject:
        reject(package, index, args.reject)
        return 0
    approve(package, index)
    return check()


if __name__ == "__main__":
    sys.exit(main())
