BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3,
	veryhard = 1
}

WorldLoaded = function()
	Camera.Position = Actor175.CenterPosition
	Beetles = Player.GetPlayer("Beetles")
	Wasps = Player.GetPlayer("Wasps")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Beetles.GrantCondition("enable-beetles-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Actor139.AttackMove(CPos.New(36,15))
		Actor135.AttackMove(CPos.New(36,15))
		Actor133.AttackMove(CPos.New(36,15))
		Actor132.AttackMove(CPos.New(36,15))
		Actor131.AttackMove(CPos.New(36,15))
		Actor138.AttackMove(CPos.New(36,15))
		Actor137.AttackMove(CPos.New(36,15))
		Actor136.AttackMove(CPos.New(36,15))
		Actor140.AttackMove(CPos.New(36,15))
	end)
end
