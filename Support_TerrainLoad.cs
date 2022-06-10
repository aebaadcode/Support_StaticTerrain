$StaticTerrain::Folder = "Add-ons/Terrain_";
//static shape datablock gen
function generateTerrainDatablocks()
{
    %pattern = $StaticTerrain::Folder @ "*";

    %set = TerrainDatablockSet;
    if(isObject(%set))
    {
        %set.clear();
    }
    else
    {
        %set = new SimSet(TerrainDatablockSet);
    }

    //grab a file and make it's datablock
    %file = findFirstFile(%pattern);
    while(%file !$= "")
    {
        %filePath = filePath(%file);
        %addonFolder = getSubStr(%file,8,strLen(%file) - 8);
        if(strstr(%addonFolder,"/") > 0)
        {
            %addonFolder = getSubStr(%addonFolder,0,strstr(%addonFolder,"/"));
        }
        //echo(%addonFolder);
        //check if it's from an enabled addon
        if($AddOnLoaded__[getSafeVariableName(%addonFolder)])
        {
            %ext = fileExt(%file);
            %fileName = fileBase(%file);
            switch$(%ext)
            {
            case ".dts":
                %firstFolder = getSubStr(%filePath,strLen(%filePath) - 3,3);
                if(%firstFolder $= "vis")
                {
                    //create a datablock for visuals
                    %dataBlockName = getSafeVariableName(%fileName) @ "Shape";
                    %datablockMaker = "datablock StaticShapeData(" @ %dataBlockName @ "){shapeFile = %file;dynamicType = $TypeMasks::TerrainObjectType;};";
                    eval(%datablockMaker);
                    //visual; add it to the set
                    %set.add(%dataBlockName);
                }
                else if(%firstFolder $= "col")
                {
                    //check if it has "col_" in the name
                    %colPos = strPos(%fileName,"col_");
                    if(%colPos != -1)
                    {
                        //collision; add it to an array for later
                        %visualName = getSafeVariableName(getSubStr(%fileName,0,%colPos)) @ "Shape";

                        addExtraResource(%file);
                        
                        %collision[%visualName,%collision[%visualName,"count"] + 0] = %file;
                        %collision[%visualName,"count"]++;
                    }
                    
                }
            case ".png":
                addExtraResource(%file);
            }
            
        }

        %file = findNextFile(%pattern);
    }

    //gather the collision datablocks we made and add them to our visual datablocks
    %visualCount = %set.getCount();
    for(%i = 0; %i < %visualCount; %i++)
    {
        //get the current datablock
        %visualData = %set.getObject(%i);
        %visualName = %visualData.getName();
        
        //use the array we made earlier to add all the collision to this datablock
        %collisionCount = %collision[%visualName,"count"];
        for(%j = 0; %j < %collisionCount; %j++)
        {
            %visualData.collisionShape[%j] = %collision[%visualName,%j];
        }

        %visualData.collisionShapeCount = %collisionCount;
    }
}

function addExtraResource(%fileName)
{
	// Don't add the same file multiple times
	if (!ServerGroup.addedExtraResource[%fileName])
	{
		// Maintain a list of "extra" files so we can work nicely with the existing
		// resources, and call PopulateEnvResourceList without getting overwritten.
		if (ServerGroup.extraResourceCount $= "")
			ServerGroup.extraResourceCount = 0;

		ServerGroup.extraResource[ServerGroup.extraResourceCount] = %fileName;
		ServerGroup.extraResourceCount++;

		ServerGroup.addedExtraResource[%fileName] = true;
	}
}

package ExtraResources
{
	function EnvGuiServer::PopulateEnvResourceList()
	{
		Parent::PopulateEnvResourceList();

		for (%i = 0; %i < ServerGroup.extraResourceCount; %i++)
		{
			$EnvGuiServer::Resource[$EnvGuiServer::ResourceCount] = ServerGroup.extraResource[%i];
			$EnvGuiServer::ResourceCount++;
		}
	}
};
activatePackage(ExtraResources);

generateTerrainDatablocks();

