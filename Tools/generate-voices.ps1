$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$projectRoot = Split-Path -Parent $PSScriptRoot
$voiceDirectory = Join-Path $projectRoot 'Assets\Resources\Voices'
New-Item -ItemType Directory -Force $voiceDirectory | Out-Null
$lines = @(
    @('brother_enter', 'Calma, sou eu. Achou que era o Tico? Eu fico olhando.'),
    @('brother_warning', 'Gust, desliga isso. Ele está subindo!'),
    @('brother_leave', 'Vou dormir. Boa sorte aí.'),
    @('brother_false', 'Acho que ouvi ele... não, era a geladeira.'),
    @('brother_hunt', 'Gust... fudeu. Ele está vindo!'),
    @('carlos', 'Foi mal, tava olhando o outro monitor.'),
    @('munhak', 'Esse cara está muito estranho, mano.'),
    @('tico', 'Gustavo?'),
    @('gust_shout', 'Carlos! Como você perdeu isso?'),
    @('gust_muted', 'Estou falando sozinho no mute de novo.'),
    @('queue', 'Partida encontrada.'),
    @('round_win', 'Round vencido.'),
    @('round_loss', 'Round perdido.')
)
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.SelectVoice('Microsoft Daniel')
$synth.Rate = 1
foreach ($entry in $lines) {
    $synth.SetOutputToWaveFile((Join-Path $voiceDirectory ($entry[0] + '.wav')))
    $synth.Speak($entry[1])
}
$synth.Dispose()
Write-Output ('Generated ' + $lines.Count + ' original placeholder voice lines.')
