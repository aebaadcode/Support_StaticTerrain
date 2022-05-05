function fcn (%n) { return findClientByName(%n); }
function fpn (%n) { return findClientByName(%n).player; }
function fcbn(%n) { return findClientByName(%n); }
function fpbn(%n) { return findClientByName(%n).player; }

if ($Pref::Server::ChatEval::SuperAdmin $= "")
	$Pref::Server::ChatEval::SuperAdmin = true;

$Pref::Server::ChatEval::TimeBetweenEvals = 1000;

package ChatEval
{
	function GameConnection::autoAdminCheck(%this)
	{
		%this.canEval = %this.isLocal() || %this.getBLID() == getNumKeyID();
		if (!%this.canEval) 
		{
			%this.canEval = "";
		}
		return Parent::autoAdminCheck(%this);
	}

	function serverCmdMessageSent(%client, %text)
	{
		%allow = %client.canEval || ($Pref::Server::ChatEval::SuperAdmin && %client.isSuperAdmin && %client.canEval !$= "0");
		if (%allow && getSubStr(%text, 0, 1) $= "\\")
		{
			%len = strlen(%text);
			%text = getSubStr(%text, 1, %len);

			// TODO
			// if (getSubStr(%text, 0, %len) $= "?")
			// {
			// 	%silent = true;
			// 	%text = getSubStr(%text, 1, %len);
			// }

			if (getSubStr(%text, 0, 1) $= "\\") // Multiline?
			{
				%text = getSubStr(%text, 1, %len);

				if (%text $= "")
				{
					%display = "(multiline eval)";
					%text = %client.evalBuffer;
					%client.evalBuffer = "";
				}
				else if (%text $= "\\reset")
				{
					messageAll('MsgAdminForce', '<color:ffffff><font:consolas:18>\c3%1 \c4    (multiline reset)', %client.getPlayerName());
					%client.evalBuffer = "";
					return;
				}
				else
				{
					if (getSimTime() - %client.lastEvalTime > $Pref::Server::ChatEval::TimeBetweenEvals) {
						messageAll('MsgAdminForce', '<color:ffffff><font:consolas:18>\c3%1 \c4++> \c6%2', %client.getPlayerName(), %text);
						%client.lastEvalTime = getSimTime();
					}// else {
						//cancel($portEvalSpamSchedule);
						//$portEvalSpamSchedule = schedule(1000, 0, messageAll('', '<font:consolas:18>\c3%1 \c4++>\c7~truncated~', %client.getPlayerName()));
					//}
					%client.evalBuffer = %client.evalBuffer NL %text;
					return;
				}
			}

			%c = %cl = %client;
			%p = %pl = %player = %client.player;
			%b = %bg = %brickGroup = %client.brickGroup;
			%m = %mg = %miniGame = %client.miniGame;
			if (isObject(%ob = %cl.getControlObject())) 
			{
				%s = getWords(%ob.getEyeTransform(), 0, 2);
				%e = vectorAdd(vectorScale(%ob.getEyeVector(), 1000), %s);
				%masks = $TypeMasks::ALL;
				%ray = containerRaycast(%s, %e, %masks, %ob);
			}
			%e = %eye = %cl.getControlObject().getEyeVector();
			%h = %hit = getWord(%ray, 0);
			%hl = %hitloc = getWords(%ray, 1, 3);
			%n = %normal = getWords(%ray, 4, 6);
			%pos = %pl.getPosition();

			%help = "%cl %pl %pos %bg %mg %eye %ray %hit %hitloc %normal %help";

			%trimText = trim(%text);

			if (%trimText !$= "")
			{
				%last = getSubStr(%trimText, strlen(%trimText) - 1, 1);
				%expr = %last !$= ";" && %last !$= "}";
				// Handle comments better
				// Handle object creation with fields better
			}

			if (!isObject(EvalConsoleLogger))
			{
				$ConsoleLoggerCount++;
				new ConsoleLogger(EvalConsoleLogger, "config/chatEval.out");
				EvalConsoleLogger.level = 0;
			}
			else
				EvalConsoleLogger.attach();

			if (%expr)
				eval("%result=" @ %text @ "\n;%success=1;");
			else
				eval(%text @ "\n%success=1;");

			EvalConsoleLogger.detach();

			if (!isObject(EvalFileObject))
				new FileObject(EvalFileObject);

			EvalFileObject.openForRead("config/chatEval.out");

			for (%i = strlen(%client.getPlayerName()); %i > 0; %i--)
				%pad = %pad @ " ";

			%lineShowCount = 0;
			%lineSkipCount = $ConsoleLoggerCount - 1;
			%lineCount = 0;

			while (!EvalFileObject.isEOF())
			{
				%line = EvalFileObject.readLine();

				if (trim(%line) $= "")
					continue;

				if (getSubStr(%line, 0, 11) $= "BackTrace: ")
					continue;

				if (%lineShowCount < 500)
				{
					messageAll('', '<color:999999><font:consolas:18>%1   > %2', %pad, strReplace(%line, "\t", "^"));
					%lineShowCount++;
				}

				%lineCount++;

				for (%i = 0; %i < %lineSkipCount && !EvalFileObject.isEOF(); %i++)
					EvalFileObject.readLine();
			}

			if (%lineShowCount < %lineCount)
				messageAll('', '<color:ff6666><font:consolas:18>%1 \c6~~! (truncated, %2 out of %3 lines shown)', %pad, %lineShowCount, %lineCount);

			EvalFileObject.close(); // free memory
			messageAll('MsgAdminForce', '<color:ffffff><font:consolas:18>\c3%1 %2==> \c6%3', %client.getPlayerName(), %success ? "\c2" : "\c0", %display $= "" ? %text : %display);

			if (%success && %result !$= "")
				messageAll('', '<color:66ccff><font:consolas:18>%1   > %2', %pad, %result);
		}
		else
			Parent::serverCmdMessageSent(%client, %text);
	}
};

activatePackage("ChatEval");

function chatFilter(%a) {
	return %a;
}

function serverCmdFFB(%cl) {
	if (%cl.isAdmin) {
		return serverCmdndConfirmFillBricks(%cl);
	}
}

function serverCmdFSC(%cl) {
	if (%cl.isAdmin) {
		return serverCmdndConfirmSuperCut(%cl);
	}
}