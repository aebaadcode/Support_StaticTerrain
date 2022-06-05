exec("./Support_TerrainLoad.cs");

if (!isObject($MainTerrainSet))
{
	$MainTerrainSet = new SimSet(MainTerrainSet);
}

//assumed StaticShape fields:
//	collisionShapeCount = number of collision shape datablocks
//	collisionShape[%i] = datablock of collisionshape to spawn when spawning this static shape

//TerrainSet fields:
//	center
//	objectSet = simset that contains terrain shape objects

//TerrainShape fields:
//	isTerrain = identifier that this is a terrain shape
//	collisionObjectSet = simset that contains terrain collision objects

//TerrainCollision fields:
//	isTerrainCollision = identifier that this is a terrain collision shape
//	terrainObj = the terrain shape object this is part of



//how to use:
//	create a terrain set with a given position using createTerrainSet(%pos)
//	call %terrainSet.createShape(%datablock, %offset, %rotation) to create the terrain objects at the given position
//	delete any shape in the terrain object to delete the entire terrain object



function createTerrainSet(%position)
{
	%terrainSet = new ScriptObject(TerrainSet);
	%set = new SimSet(CommandTerrainObjectSet);

	%terrainSet.objectSet = %set;
	%terrainSet.center = vectorAdd(%position, "0 0 0"); //initializes the value if none is provided
	$MainTerrainSet.add(%terrainSet);
	
	MissionCleanup.add(%set);
	MissionCleanup.add(%terrainSet);
	return %terrainSet;
}

//accepts just %vec, or %x %y %z values
function TerrainSet::getOffsetPosition(%terrainSet, %vec, %y, %z)
{
	//check if only one value is passed in, or 3
	if (%y !$= "" || %z !$= "")
	{
		%vec = %vec SPC %y SPC %z;
	}
	return vectorAdd(%terrainSet.center, %vec);
}

function TerrainSet::onRemove(%terrainSet)
{

}

//Creates a terrain shape at the %vec position, relative to the terrain object center
//@params:
//	%vec = offset in TU
//	%rot = number in [0, 3] corresponding to rotation of shape
function TerrainSet::createShape(%terrainSet, %shape, %vec, %rot, %scale)
{
	if (getWordCount(%scale) < 3)
	{
		if (%scale <= 0)
		{
			%scale = "1 1 1";
		}
		else
		{
			%scale = %scale SPC %scale SPC %scale;
		}
	}
	%terrainObj = new StaticShape(TerrainShape)
	{
		dataBlock = %shape;
		isTerrain = 1;
	};
	%position = %terrainSet.getOffsetPosition(%vec);

	if(getWordCount(%rot) == 3)
	{
		%rot = eulerToMatrix(%rot);
		%rot = getWords(%rot, 0, 2) SPC mDegToRad(getWord(%rot, 3));
	}
	else if(getWordCount(%rot) == 1)
	{
		switch (%rot)
		{
			case 0: %rot = "1 0 0 0";
			case 1: %rot = "0 0 1 " @ 90 / 180 * 3.14159;
			case 2: %rot = "0 0 1 " @ 180 / 180 * 3.14159;
			case 3: %rot = "0 0 1 " @ 270 / 180 * 3.14159;
			default: %rot = "1 0 0 0";
		}
	}

	%transform = %position SPC %rot;
	%terrainObj.setTransform(%transform);
	%terrainObj.setScale(%scale);
	%realScale = vectorScale(%scale, 0.5);

	%terrainSet.objectSet.add(%terrainObj);

	for (%i = 0; %i < %shape.collisionShapeCount; %i++)
	{
		// %collisionObj = new StaticShape(TerrainCollisionShape)
		%collisionObj = new TSStatic(TerrainCollisionShape)
		{
			// dataBlock = %shape.collisionShape[%i];
			shapeName = %shape.collisionShape[%i];
			terrainObj = %terrainObj;
			isTerrainCollision = 1;

			//"eagle is telling me it should use the position and rotations fields" (instead of setTransform())
			position = %position;
			rotation = getWords(%rot, 0, 2) SPC mRadToDeg(getWord(%rot, 3));
		};
		%collisionObj.setScale(%realScale);
		%collisionObj.setType(%collisionObj.getType() | $TypeMasks::StaticShapeObjectType | $TypeMasks::TerrainObjectType);

		if (!isObject(%terrainObj.collisionObjectSet))
		{
			%terrainObj.collisionObjectSet = new SimSet(TerrainShapeCollisionSet);
			MissionCleanup.add(%terrainObj.collisionObjectSet);
		}
		%terrainObj.collisionObjectSet.add(%collisionObj);
	}
	
	return %terrainObj;
}



function moveTerrainChunk(%obj, %offset)
{
    if (%obj.isTerrainCollision)
    {
        %visual = %obj.terrainObj;
    }
    else if (%obj.isTerrain && !%obj.chainRemoved)
    {
        %visual = %obj;
    }

    // if (isObject(%visual.collisionObjectSet))
    // {
    //     for (%i = 0; %i < %visual.collisionObjectSet.getCount(); %i++)
    //     {
    //         %shape = %visual.collisionObjectSet.getObject(%i);
    //         %rot = getWords(%shape.getTransform(), 3, 6);
    //         %shape.setTransform(vectorAdd(%shape.getPosition(), %offset) SPC %rot);
    //     }
    // }
    %rot = getWords(%visual.getTransform(), 3, 6);
    // %visual.setTransform(vectorAdd(%visual.getPosition(), %offset) SPC %rot);
    %newpos = vectorAdd(%visual.getPosition(), %offset);
    %newscale = %visual.getScale();

    %shape = %visual.getDataBlock();
    %ref = %visual.reference;

    deleteTerrainChunk(%obj);
    %obj = createTerrainSet().createShape(%shape, %newpos, %rot, %newscale);
    %obj.reference = %ref;
    $StaticTerrain::CommandTerrainObject[%ref] = %obj;
    CommandTerrainSet.add(%obj);
}

function deleteTerrainChunk(%obj)
{
	if (%obj.isTerrainCollision)
	{
		%visual = %obj.terrainObj;
	}
	else if (%obj.isTerrain && !%obj.chainRemoved)
	{
		%visual = %obj;
	}

	if (isObject(%visual.collisionObjectSet))
	{
		while (%visual.collisionObjectSet.getCount() > 0)
		{
			%shape = %visual.collisionObjectSet.getObject(0);
			%shape.delete();
		}
		%visual.collisionObjectSet.delete();
	}
	%visual.chainRemoved = 1;
	%visual.delete();
}

if(!isObject(CommandTerrainSet))
{
	new SimSet(CommandTerrainSet);
}

function addAddTerrainCommand(%name,%args)
{
	if(!$StaticTerrain::CommandExists[%name])
	{
		$StaticTerrain::CommandExists[%name] = true;
		$StaticTerrain::Command[$StaticTerrain::Command["Count"] + 0] = %name TAB %args;
		$StaticTerrain::Command["Count"]++;
	}
}

function serverCmdTerrainHelp(%client)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	%count = $StaticTerrain::Command["Count"];
	%client.chatMessage("\c2Terrain Help:");
	%client.chatMessage("\c2You can use ALL as a reference to target all terrain");
	for(%i = 0; %i < %count; %i++)
	{
		%field = $StaticTerrain::Command[%i];
		%name = getField(%field,0);
		%args = getField(%field,1);
		%client.chatMessage("\c3/" @ %name SPC "\c4" @ %args);
	}
}

addAddTerrainCommand("MakeTerrain", "reference fileName");
function serverCmdMakeTerrain(%client,%reference,%w1,%w2,%w3,%w4,%w5,%w6,%w7,%w8,%w9,%w10)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	%string = trim(%w1 SPC %w2 SPC %w3 SPC %w4 SPC %w5 SPC %w6 SPC %w7 SPC %w8 SPC %w9 SPC %w10);
	%datablock = getSafeVariableName(%string) @ "Shape";

	if(isObject(%dataBlock))
	{
		if(!isObject($StaticTerrain::CommandTerrainObject[%reference]))
		{
			%obj = createTerrainSet().createShape(%datablock);
			%obj.reference = %reference;
			$StaticTerrain::CommandTerrainObject[%reference] = %obj;
			CommandTerrainSet.add(%obj);
		}
		else
		{
			%client.chatMessage("Reference already exists");
		}
	}
	else
	{
		%client.chatMessage("Invalid terrain name");
	}
}

function serverCmdCreateTerrain(%c,%r,%w1,%w2,%w3,%w4,%w5,%w6,%w7,%w8,%w9,%w10) { return serverCmdMakeTerrain(%c,%r,%w1,%w2,%w3,%w4,%w5,%w6,%w7,%w8,%w9,%w10); }

addAddTerrainCommand("MoveTerrain", "reference X Y Z");
function serverCmdMoveTerrain(%client,%reference,%x,%y,%z)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	if(%reference $= "ALL")
	{
		%count = CommandTerrainSet.getCount();
		for(%i = 0; %i < %count; %i++)
		{
			serverCmdMoveTerrain(%client,CommandTerrainSet.getObject(%i).reference, %x, %y, %z);
		}
	}
	else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
	{
		moveTerrainChunk($StaticTerrain::CommandTerrainObject[%reference], %x SPC %y SPC %z);
	}
	else
	{
		%client.chatMessage("Invalid reference name");
	}
}


addAddTerrainCommand("SetMoveTerrain", "reference X Y Z");
function serverCmdSetMoveTerrain(%client,%reference,%x,%y,%z)
{
    if(!%client.isSuperAdmin)
    {
        return;
    }

    %player = %client.player;
    if(!isObject(%player))
    {
        return;
    }

    if(%reference $= "ALL")
    {
        %count = CommandTerrainSet.getCount();
        for(%i = %count - 1; %i >= 0; %i--)
        {
            serverCmdSetMoveTerrain(%client,CommandTerrainSet.getObject(%i).reference,%x,%y,%z);
        }
    }
    else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
    {
        %setPos = trim(%x SPC %y SPC %z);
        %terrainPos = $StaticTerrain::CommandTerrainObject[%reference].getPosition();
        %relMove = vectorSub(%setPos,%terrainPos);
        serverCmdMoveTerrain(%client,%reference,getWord(%relMove,0),getWord(%relMove,1),getWord(%relMove,2));
    }
    else
    {
        %client.chatMessage("Invalid reference name");
    }
}

addAddTerrainCommand("rotateTerrain", "reference [angleID(0-3) | rX rY rZ]");
function serverCmdrotateTerrain(%client,%reference,%rX, %rY, %rZ)
{
    if(!%client.isSuperAdmin)
    {
        return;
    }

    if(%reference $= "ALL")
    {
        %count = CommandTerrainSet.getCount();
        for(%i = 0; %i < %count; %i++)
        {
            serverCmdrotateTerrain(%client,CommandTerrainSet.getObject(%i).reference, %rX, %rY, %rZ);
        }
    }
    else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
    {
			if(%rY $= "")
      	rotateTerrainChunk($StaticTerrain::CommandTerrainObject[%reference], %rX);
			else
				rotateTerrainChunk($StaticTerrain::CommandTerrainObject[%reference], %rX SPC %rY SPC %rZ);
    }
    else
    {
        %client.chatMessage("Invalid reference name");
    }
}

function rotateTerrainChunk(%obj, %newRot)
{
    if (%obj.isTerrainCollision)
    {
        %visual = %obj.terrainObj;
    }
    else if (%obj.isTerrain && !%obj.chainRemoved)
    {
        %visual = %obj;
    }

    %newpos = %visual.getPosition();
    %newscale = %visual.getScale();

    %shape = %visual.getDataBlock();
    %ref = %visual.reference;

    deleteTerrainChunk(%obj);
    %obj = createTerrainSet().createShape(%shape, %newpos, %newRot, %newscale);
    %obj.reference = %ref;
    $StaticTerrain::CommandTerrainObject[%ref] = %obj;
    CommandTerrainSet.add(%obj);
}

addAddTerrainCommand("ColorTerrain", "reference R G B A");
function serverCmdColorTerrain(%client,%reference,%r,%g,%b,%a)
{
    if(!%client.isSuperAdmin)
    {
        return;
    }

    if(%reference $= "ALL")
    {
        %count = CommandTerrainSet.getCount();
        for(%i = 0; %i < %count; %i++)
        {
            serverCmdColorTerrain(%client,CommandTerrainSet.getObject(%i).reference, %r, %g, %b, %a);
        }
    }
    else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
    {
        if(%a $= "")
            %a = 1;
        
        if(%r $= "")
            %col = "1 1 1";
        else
            %col = vectorAdd(%r SPC %g SPC %b, "0 0 0");

        %a = mClampF(%a, 0, 1);

        $StaticTerrain::CommandTerrainObject[%reference].setNodeColor("ALL", %col SPC %a);
		$StaticTerrain::CommandTerrainObject[%reference].currNodeColor = %col SPC %a;

        if(%a < 1.0)
            $StaticTerrain::CommandTerrainObject[%reference].startFade(1,0,1);
        else
            $StaticTerrain::CommandTerrainObject[%reference].startFade(0,0,0);
    }
    else
    {
        %client.chatMessage("Invalid reference name");
    }
}

addAddTerrainCommand("BringTerrain", "reference");
function serverCmdBringTerrain(%client,%reference)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	%player = %client.player;
	if(!isObject(%player))
	{
		return;
	}

	if(%reference $= "ALL")
	{
		%count = CommandTerrainSet.getCount();
		for(%i = %count - 1; %i >= 0; %i--)
		{
			serverCmdBringTerrain(%client,CommandTerrainSet.getObject(%i).reference);
		}
	}
	else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
	{
		%playerPos = %player.getPosition();
		serverCmdSetMoveTerrain(%client,%reference,getWord(%playerPos,0),getWord(%playerPos,1),getWord(%playerPos,2));
	}
	else
	{
		%client.chatMessage("Invalid reference name");
	}
}

addAddTerrainCommand("ScaleTerrain", "reference X Y Z");
function serverCmdScaleTerrain(%client,%reference,%x,%y,%z)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	if(%reference $= "ALL")
	{
		%count = CommandTerrainSet.getCount();
		for(%i = 0; %i < %count; %i++)
		{
			serverCmdScaleTerrain(%client,CommandTerrainSet.getObject(%i).reference, %x, %y, %z);
		}
	}
	else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
	{
		//get old position and datablock
		%pos = $StaticTerrain::CommandTerrainObject[%reference].getPosition();
		%dataBlock = $StaticTerrain::CommandTerrainObject[%reference].getDatablock();

		//easiest way to scale is delete the old one and create a new one
		deleteTerrainChunk($StaticTerrain::CommandTerrainObject[%reference]);

		%obj = createTerrainSet().createShape(%dataBlock, %pos, "", %x SPC %y SPC %z);
		%obj.reference = %reference;
		$StaticTerrain::CommandTerrainObject[%reference] = %obj;
		CommandTerrainSet.add(%obj);
	}
	else
	{
		%client.chatMessage("Invalid reference name");
	}
}

addAddTerrainCommand("SkinTerrain", "reference skinName");
function serverCmdSkinTerrain(%client,%reference,%skin)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	if(%reference $= "ALL")
	{
		%count = CommandTerrainSet.getCount();
		for(%i = 0; %i < %count; %i++)
		{
			serverCmdSkinTerrain(%client,CommandTerrainSet.getObject(%i).reference, %skin);
		}
	}
	else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
	{
		$StaticTerrain::CommandTerrainObject[%reference].setSkinName(%skin);
	}
	else
	{
		%client.chatMessage("Invalid reference name");
	}
}

addAddTerrainCommand("DeleteTerrain", "reference");
function serverCmdDeleteTerrain(%client,%reference)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	if(%reference $= "ALL")
	{
		%count = CommandTerrainSet.getCount();
		for(%i = %count - 1; %i >= 0; %i--)
		{
			serverCmdDeleteTerrain(%client,CommandTerrainSet.getObject(%i).reference);
		}
	}
	else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
	{
		serverCmdRemoveLoopTerrain(%client,%reference);
		deleteTerrainChunk($StaticTerrain::CommandTerrainObject[%reference]);
		$StaticTerrain::CommandTerrainObject[%reference] = "";
	}
	else
	{
		%client.chatMessage("Invalid reference name");
	}
}

addAddTerrainCommand("LoopTerrain", "reference rows columns");
function serverCmdLoopTerrain(%client,%reference,%x,%y)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	if(%reference $= "ALL")
	{
		%count = CommandTerrainSet.getCount();
		for(%i = %count - 1; %i >= 0; %i--)
		{
			serverCmdLoopTerrain(%client,CommandTerrainSet.getObject(%i).reference,%x,%y);
		}
	}
	else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
	{
		%terrain = $StaticTerrain::CommandTerrainObject[%reference];
		//get size of this terrain
		%worldBox = %terrain.getWorldBox();
		%lowerBound = getWords(%worldBox,0,2);
		%upperBound = getWords(%worldBox,3);

		%zeroRelativeBound = vectorSub(%upperBound,%lowerBound);

		//make a group for the looped terrain
		if(!isObject(%terrain.loopTerrain))
		{
			%terrain.loopTerrain = createTerrainSet();
		}
		%loopTerrain = %terrain.loopTerrain;

		if(%loopTerrain.objectSet.getCount() > 0)
		{
			serverCmdRemoveLoopTerrain(%client,%reference);
		}

		//get attributes of the parent terrain
		%skinName = %terrain.getSkinName();
		%nodeColor = %terrain.currNodeColor;

		//make the looped terrain
		%startPosition = %terrain.getPosition();
		%dataBlock = %terrain.getDataBlock();
		for(%ix = 0; %ix < %x; %ix++)
		{
			for(%iy = 0; %iy < %y; %iy++)
			{
				//skip initial spot
				if(%ix != 0 || %iy != 0)
				{
					%newX = getWord(%startPosition,0) + getWord(%zeroRelativeBound,0) * %ix;
					%newY = getWord(%startPosition,1) + getWord(%zeroRelativeBound,1) * %iy;
					%newPosition = %newX SPC %newY SPC getWord(%startPosition,2);

					%newTerrain = %loopTerrain.createShape(%dataBlock,%newPosition);
					if(%skinName !$= "")
					{	
						%newTerrain.setSkinName(%skinName);
					}
					
					if(%nodeColor !$= "")
					{
						%newTerrain.setNodeColor("ALL",%nodeColor);
					}
				}
			}
		}

		%terrain.loopx = %x;
		%terrain.loopy = %y;
	}
	else
	{
		%client.chatMessage("Invalid reference name");
	}
}

addAddTerrainCommand("RemoveLoopTerrain", "reference");
function serverCmdRemoveLoopTerrain(%client,%reference,%x,%y)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	if(%reference $= "ALL")
	{
		%count = CommandTerrainSet.getCount();
		for(%i = %count - 1; %i >= 0; %i--)
		{
			serverCmdRemoveLoopTerrain(%client,CommandTerrainSet.getObject(%i).reference);
		}
	}
	else if(isObject($StaticTerrain::CommandTerrainObject[%reference]))
	{
		%terrain = $StaticTerrain::CommandTerrainObject[%reference];
		%loopSet = %terrain.loopTerrain.objectSet;
		if(!isObject(%loopSet))
		{
			return;
		}

		%count = %loopSet.getCount();
		for(%i = %count - 1; %i >= 0; %i--)
		{
			deleteTerrainChunk(%loopSet.getObject(%i));
		}

		%terrain.loopx = 0;
		%terrain.loopy = 0;
	}
	else
	{
		%client.chatMessage("Invalid reference name");
	}
}

$Terrain::SaveLocation = "config/server/TerrainSaves/";

addAddTerrainCommand("SaveTerrain","filename");
function serverCmdSaveTerrain(%client,%filename)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	%filename = getSafeVariableName(%fileName);

	%file = new FileObject();
	%success = %file.openForWrite($Terrain::SaveLocation @ %fileName @ ".txt");
	if(%success)
	{
		%client.chatMessage("\c6Saving file \c3" @ $Terrain::SaveLocation @ %fileName @ ".txt");
		//loop through the command terrain group and serialize string them then write them as a line
		//datablock TAB reference TAB position TAB scale TAB rotation TAB skin TAB color TAB loopx TAB loopy
		%group = CommandTerrainSet;
		%count = CommandTerrainSet.getCount();
		for(%i = 0; %i < %count; %i++)
		{
			%curr = CommandTerrainSet.getObject(%i);

			%datablock = %curr.getDatablock().getName();
			%datablock = getSubStr(%datablock,0,strStr(%dataBlock,"Shape"));
			%reference = %curr.reference;
			%position = %curr.getPosition();
			%scale = %curr.getScale();
			%rotation = %curr.rotation;
			%skin = %curr.getSkinName();
			%color = %curr.currNodeColor;
			%loopx = %curr.loopx;
			%loopy = %curr.loopy;

			%file.writeLine(%dataBlock TAB %reference TAB %position TAB %scale TAB %rotation TAB %skin TAB %color TAB %loopx TAB %loopy);
		}
	}
	else
	{
		%client.chatMessage("Failed to open file \c3" @ $Terrain::SaveLocation @ %fileName @ ".txt");
	}

	%file.close();
	%file.delete();
}

addAddTerrainCommand("LoadTerrain","filename");
function serverCmdLoadTerrain(%client,%filename)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	%filename = getSafeVariableName(%fileName);

	%file = new FileObject();
	%success = %file.openForRead($Terrain::SaveLocation @ %fileName @ ".txt");
	if(%success)
	{
		%client.chatMessage("\c6Loading file \c3" @ $Terrain::SaveLocation @ %fileName @ ".txt");
		//loop through and read lines creating terrain in the process
		//datablock TAB reference TAB position TAB scale TAB rotation TAB skin TAB color TAB loopx TAB loopy
		while(!%file.isEOF())
		{
			%line = %file.readLine();

			%c = -1;
			%datablock = getField(%line,%c++);
			%reference = getField(%line,%c++);
			%position = getField(%line,%c++);
			%scale = getField(%line,%c++);
			%rotation = getField(%line,%c++);
			%skin = getField(%line,%c++);
			%color = getField(%line,%c++);
			%loopx = getField(%line,%c++);
			%loopy = getField(%line,%c++);

			serverCmdMakeTerrain(%client,%reference,%dataBlock);
			serverCmdSetMoveTerrain(%client,%reference,%position);
			serverCmdScaleTerrain(%client,%reference,%scale);
			rotateTerrainChunk($StaticTerrain::CommandTerrainObject[%reference], getWords(%rotation,0,2) SPC getWord(%rotation,3) * ($PI / 180));
			serverCmdSkinTerrain(%client,%reference,%skin);
			serverCmdColorTerrain(%client,%reference,%color);
			serverCmdLoopTerrain(%client,%reference,%loopx,%loopy);
		}
	}
	else
	{
		%client.chatMessage("Failed to open file \c3" @ $Terrain::SaveLocation @ %fileName @ ".txt");
	}

	%file.close();
	%file.delete();
}

function serverCmdRemoveTerrain(%c,%r) { return serverCmdDeleteTerrain(%c,%r); }
function serverCmdClearTerrain(%c,%r) { return serverCmdDeleteTerrain(%c,%r); }

addAddTerrainCommand("ToggleTerrainReferences");
function serverCmdToggleTerrainReferences(%client)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}

	%visible = $StaticTerrain::CommandTerrainReferencesVisible = !$StaticTerrain::CommandTerrainReferencesVisible;
	%count = CommandTerrainSet.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%obj = commandTerrainSet.getObject(0);
		if(%visible)
		{
			%name = %obj.reference;
		}
		%obj.setShapeName(%name);
	}
}

addAddTerrainCommand("ListTerrainReferences");
function serverCmdListTerrainReferences(%client)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}
	
	%client.chatMessage("\c0Reference Name, \c1File Name, \c2Skin Name, \c3Scale, \c4Position");
	%count = CommandTerrainSet.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%obj = commandTerrainSet.getObject(%i);
		%name = %obj.reference;

		%shapeName = %obj.getDatablock().getName();
		%shapeName = getSubStr(%shapeName,0,strLen(%shapeName) - 5);

		%skinName = %obj.getSkinName();
		if(%skinName $= "")
		{
			%skinName = "Default";
		}

		%scale = %obj.getScale();
		
		%pos = %obj.getPosition();

		%client.chatMessage("\c0" @ %name @ "\c6,\c1" SPC %shapeName @ "\c6,\c2" SPC %skinName @ "\c6,\c3" SPC %scale  @ "\c6,\c4" SPC %pos);
	}
}

addAddTerrainCommand("ListTerrain");
function serverCmdListTerrain(%client)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}
	
	%count = TerrainDatablockSet.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%obj = TerrainDatablockSet.getObject(%i);

		%name = %obj.getName();
		%name = getSubStr(%name,0,strLen(%name) - 5);

		%client.chatMessage("\c0" @ %name);
	}
}

addAddTerrainCommand("ListTerrainSkin");
function serverCmdListTerrainSkin(%client, %name)
{
    if(!%client.isSuperAdmin)
    {
        return;
    }
    
    %dataBlock = %name @ "Shape";
    if(isObject(%dataBlock))
    {
        %filepath = filePath(%dataBlock.shapeFile);
        %filepath = getSubStr(%filepath,0,strLen(%filepath) - 3);
        //look for .png files
        %currFile = findFirstFile(%filePath @ "*.png");
        while(isFile(%currFile))
        {
            %skinName = fileBase(%currFile);
            %skinName = getSubStr(%skinName,0,strStr(%skinName,"."));

            %client.chatMessage("\c0" @ %skinName);

            %currFile = findNextFile(%filePath @ "*.png");
        }
    }
    else
    {
        %client.chatMessage("Invalid terrain name");
    }
}

addAddTerrainCommand("TerrainDestroy");
function serverCmdTerrainDestroy(%client)
{
	if(!%client.isSuperAdmin)
	{
		return;
	}
	
    %group = MissionCleanup;
    %count = %group.getCount();
    for(%i = %count - 1; %i >= 0; %i--)
    {
        %obj = %group.getObject(%i);
        if(%obj.getClassName() $= "TSStatic")
        {
            %obj.delete();
        }

        if(%obj.getClassName() $= "StaticShape")
        {
            %obj.delete();
        }
    }
}