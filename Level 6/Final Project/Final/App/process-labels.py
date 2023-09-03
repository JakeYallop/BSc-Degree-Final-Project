import json

with open("efficientnet_labels.txt") as f:
    doc = json.loads(f.read())
    items = []
    for i in range(len(doc)):
        item = doc[str(i)]
        items.append(f"{item}\n")
    with open("efficientnet_labels_processed.txt", mode="w") as fw:
        fw.writelines(items)
