BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3,
	veryhard = 1
}

WorldLoaded = function()
	Camera.Position = Actor91.CenterPosition
	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beetles-ai")
		Actor38.Attack(Actor62)
		Actor43.Attack(Actor62)
	end)
end
