import csv
import pprint
import copy
import os
import sys

op = os.path
pp = pprint.PrettyPrinter(indent=2)

#TUNING contains Items.csv
#text contains Strings.csv
GamePaths = {
    'TUNING'     :   '../../Necropolis_Data/StreamingAssets/data/TUNING/',
    'text'   :   '../../Necropolis_Data/StreamingAssets/data/text/'}

FileNames = {
    'Items'             :   'Items.csv',
    'Strings'           :   'Strings.csv',
    'Template'          :   'Template.csv',
    'Eetems'            :   'Eetems.csv',
    'NecroForgeStrings' :   'NecroForgeStrings.csv'}


#####################################################################
################    GET FIELD NAMES  ################################
# filePath: Name of file to collect dictionary field names for
#####################################################################
def GetFieldNames(filePath,fileName):
    print('\n Getting Field Names...')
    with open(op.join(GamePaths[filePath], FileNames[fileName]), newline='', encoding='utf8') as csvfile:
        reader = csv.DictReader(csvfile, delimiter=',', dialect=csv.excel, quoting=csv.QUOTE_NONE)
        for field in reader.fieldnames:
            if field is '' or field is "" or field is None:
                print('Removed: ')
                del field        
    return reader.fieldnames

#####################################################################
################    GET COMPONENT TEMPLATE  #########################
# filePath: path
#####################################################################
def GetComponentTemplate(fileName, fieldNames):
    # Getting a component entry that already exists
    with open(op.join(FileNames[fileName]), newline='', encoding='utf8') as csvfile:
        reader = csv.DictReader(csvfile, fieldnames=fieldNames)
        for row in reader:
            if row['Group'] == 'Component':
                for entry in row:
                    templateComponent = dict(row)
                    continue
    return templateComponent


#####################################################################
################    WALK DIRECTORIES  ###############################
# fileName: Name of file to collect paths for in Mods folder
#####################################################################
def WalkDirectories(fileName):
    print('\n Walking Directories...')
    itemCSVPaths = []
    for root, dirs, files in os.walk("../", True):
        for name in dirs:
            if op.exists(op.join(root, name, FileNames[fileName])):
                #print(op.join(root, name) + " added to the list.")
                itemCSVPaths.append(op.join(root, name, FileNames[fileName]))
    return itemCSVPaths


#####################################################################
################    GET COMPONENTS  #################################
# itemCSVPaths: list of paths to Items.csv files
# fieldNames: dictionary field names
#####################################################################
def GetComponents(itemCSVPaths, fieldNames):
    print('\n Getting Components...')
    items = []
    # For every Items.csv in the list of Items.csv files,
    for itemCSV in itemCSVPaths:
        print("Scanning" + itemCSV)
        with open(itemCSV , newline='', encoding='utf8') as csvfile:
            reader = csv.DictReader(csvfile, fieldnames=fieldNames)
            for row in reader:
                if 'Crafting Recipe' in row and row['Crafting Recipe'] != None and row['Crafting Recipe'] != '':
                    if '_Comp' in row['Crafting Recipe']:
                        ingredients = row['Crafting Recipe'].split(',')
                        for i in ingredients:
                            if '_Comp' in i:
                                items.append(i.replace('_Comp','').strip())
    items = list(set(items))
    return items


def MakeComponents(itemComps, fieldNames):
    templateComponent = GetComponentTemplate('Template',fieldNames)
    itemCompEntries = []
    for str in itemComps:
        newComponent = copy.deepcopy(templateComponent)
        str = 'patchOver_' + str + '_Comp'
        newComponent['ID'] = str
        print(newComponent['ID'])
        itemCompEntries.append(newComponent)
    return itemCompEntries
    
#####################################################################
##################    WRITE ITEMS  ##################################
# itemComps: list of item components
# fieldNames: dictionary field names
#####################################################################
def WriteItems(itemComps, fieldNames):
    # Gotta collect the current items file into a dictionary
    # then write that shit to the file,
    # then write the itemStrings to the file that don't already exist
    ItemCSVOrigContents = []
    with open(FileNames['Items'], newline='', encoding='utf8') as csvfile:
        reader = csv.DictReader(csvfile, fieldnames=fieldNames)
        for row in reader:
            if row['ID'] != '':
                ItemCSVOrigContents.append(row)

    print('\n Making itemCompEntries...')
    itemCompEntries = MakeComponents(itemComps, fieldNames)

    with open('outf.csv', 'w', newline='', encoding='utf8') as outf:
        writer = csv.DictWriter(outf, fieldnames=fieldNames)
        print('\n Writing Eetems...')
        for entry in ItemCSVOrigContents:
            writer.writerow(entry)

        #for row in itemCompEntries:
            #for entry in ItemCSVOrigContents:
        for row in itemCompEntries:
            for entry in ItemCSVOrigContents:
                if row['ID'] == entry['ID']:
                    itemCompEntries.remove(row)
            writer.writerow(row)


    if op.isfile(FileNames['Items']):
       os.remove(FileNames['Items'])
    os.replace(outf.name, FileNames['Items'])

    return


#####################################################################
################    GET ITEM STRINGS    #############################
# stringCSVPaths: list of paths to Strings.csv files
# fieldNames: dictionary field names
# itemComps: component versions of items ( BaseLongsword_Comp )
#####################################################################
def GetItemStrings(stringCSVPaths, fieldNames, itemComps):
    print('\n Getting Usual Strings...')

    print("Scanning" + op.join(GamePaths['text'], FileNames['Strings']))
    #Get all the strings associated with itemComps equipment equivalents
    # from the game's files
    defaultStrings = []
    with open(op.join(GamePaths['text'], FileNames['Strings']), newline='', encoding='utf8') as csvfile:
        reader = csv.DictReader(csvfile, fieldnames=fieldNames)
        for row in reader:
            for itemComp in itemComps:
                if itemComp.strip() in row['ID']:
                    defaultStrings.append(row)

    #Get all the strings associated with itemComps equipment equivalents
    # from the mod files
    modStrings = []
    for stringCSV in stringCSVPaths:
        if 'NecroForgeCSV' in stringCSV:
            continue
        print("Scanning" + stringCSV)
        with open(stringCSV, newline='', encoding='utf8') as csvfile:
            reader = csv.DictReader(csvfile, fieldnames=fieldNames)
            for row in reader:
                for itemComp in itemComps:
                    if itemComp.strip() in row['ID']:
                        modStrings.append(row)
    
    # Remove redundant and misguided patch prefixes for now.
    for str in modStrings:
        if 'patchOver_' in row['ID'] or 'patchAdd_' in row['ID']:
            #print(str['ID'])
            splitID = str['ID'].partition('_')
            str['ID'] = splitID[2]

    return defaultStrings + modStrings


#####################################################################
############    GET NECRO FORGE INGREDIENT STRINGS   ################
# fieldNames: dictionary field names
#####################################################################
def GetNFIngredientStrings(fieldNames):
    stringsNF = []
    with open(FileNames['NecroForgeStrings'], newline='', encoding='utf8') as csvfile:
        reader = csv.DictReader(csvfile, fieldnames=fieldNames)
        for row in reader:
            stringsNF.append(row)
    return stringsNF


#####################################################################
################    WRITE STRINGS    ################################
# itemComps: list of the component versions of equipment items
# fieldNames: dictionary field names
#####################################################################
def WriteStrings(itemComps, fieldNames, itemStrings):
    # Gotta collect the current strings file into a dictionary
    # then write that shit to the file,
    # then write the itemStrings to the file that don't already exist
    originalStringContents = []
    with open(FileNames['NecroForgeStrings'], newline='', encoding='utf8') as csvfile:
        reader = csv.DictReader(csvfile, fieldnames=fieldNames)
        for row in reader:
            if row['ID'] != '':
                originalStringContents.append(row)

    #print('\n Writing Strings...')
    for str in itemStrings:
        splitID = str['ID'].partition('/')
        str['ID'] = 'patchOver_' + splitID[0] + '_Comp' + splitID[1] + splitID[2]
        

    with open('outf.csv', 'w', newline='', encoding='utf8') as outf:
        writer = csv.DictWriter(outf, fieldnames=fieldNames)
        print('\n Writing Strings...')
        stringsNF = GetNFIngredientStrings(fieldNames)
        for str in stringsNF:
            writer.writerow(str)
        for str in itemStrings:
            writer.writerow(str)

    if op.isfile(FileNames['Strings']):
       os.remove(FileNames['Strings'])
    os.replace(outf.name, FileNames['Strings'])

    return


print("################ START ################")
stringFieldNames = GetFieldNames('text','Strings')
itemFieldNames = GetFieldNames('TUNING','Items')

itemComps = GetComponents(WalkDirectories('Items'), itemFieldNames)

strings = GetItemStrings(WalkDirectories('Strings'), stringFieldNames, itemComps)

WriteItems(itemComps, itemFieldNames)

WriteStrings(itemComps, stringFieldNames, strings)

print("################ END ################")