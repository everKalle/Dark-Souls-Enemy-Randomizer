finalStr = ""

with open('shite.txt', 'r') as f:
    for i,line in enumerate(f):
        #print(str(i) + " - " + line.strip())
        finalStr += '"' + line.strip() + '", '

print(finalStr)