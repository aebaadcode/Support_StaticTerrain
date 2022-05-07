// \new StaticShape(terraintest){datablock = visualshape;}; 
// \terraintest.setScale("10 10 10");
// \terraintest.setScale("0.5 0.5 0.5");
// \terraintest.setSkinName("skin");
// \terraintest.setNodeColor("ALL", "1 1 1 1"); \terraintest.startFade(1,0,1); TO FIX TRANSPARENCY
//terraintest.setTransform("0 0 0"); terraintest.setScale("0 0 0");

// COLLISION TESTING

$StaticTerrain::Folder = "Add-ons/Support_StaticTerrain/terraintest";
//static shape datablock gen
function generateTerrainDatablocks()
{
    %pattern = $StaticTerrain::Folder @ "/*.dts";

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
        %fileName = fileBase(%file);
        
        //create a datablock for visuals
        %dataBlockName = getSafeVariableName(%fileName) @ "Shape";
        %datablockMaker = "datablock StaticShapeData(" @ %dataBlockName @ "){shapeFile = %file;dynamicType = $TypeMasks::TerrainObjectType;};";

        //did we just make a visual datablock or collision?
        %path = filePath(%file);
        if(%path $= $StaticTerrain::Folder)
        {
		        eval(%datablockMaker);
            //visual; add it to the set
            %set.add(%dataBlockName);
        }
        else
        {
            //check if it has "col_" in the name
            %colPos = strPos(%fileName,"col_");
            if(%colPos != -1)
            {
                //collision; add it to an array for later
                %visualName = getSafeVariableName(getSubStr(%fileName,0,%colPos)) @ "Shape";

								addExtraResource(%file);
                                talk(%file);
				
                %collision[%visualName,%collision[%visualName,"count"] + 0] = %file;
                %collision[%visualName,"count"]++;
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
        echo(%collisionCount SPC "count for" SPC %visualName);
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

addExtraResource("Add-Ons/Support_StaticTerrain/asphalt.ground.png");
addExtraResource("Add-Ons/Support_StaticTerrain/board.ground.png");
addExtraResource("Add-Ons/Support_StaticTerrain/brickTOP.ground.png");
addExtraResource("Add-Ons/Support_StaticTerrain/cement.ground.png");
addExtraResource("Add-Ons/Support_StaticTerrain/dirt.ground.png");
addExtraResource("Add-Ons/Support_StaticTerrain/dirt2.ground.png");
addExtraResource("Add-Ons/Support_StaticTerrain/realgrass.ground.png");
addExtraResource("Add-Ons/Support_StaticTerrain/discord.terrain.png");
addExtraResource("Add-Ons/Support_StaticTerrain/funny.terrain.png");

generateTerrainDatablocks();

