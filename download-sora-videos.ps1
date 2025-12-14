# Download Videos from Completed Sora Jobs
# This script retrieves and downloads videos from already completed Sora video generation jobs

param(
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile = "appsettings.json",
    
    [Parameter(Mandatory=$false)]
    [string]$JobId,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDirectory = ".",
    
    [Parameter(Mandatory=$false)]
    [switch]$ListJobs,
    
    [Parameter(Mandatory=$false)]
    [int]$LastNJobs = 10
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Sora Video Download Utility" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Load configuration
if (-not (Test-Path $ConfigFile)) {
    Write-Host "❌ Configuration file not found: $ConfigFile" -ForegroundColor Red
    Write-Host "Please run provision-sora-model.ps1 first or specify -ConfigFile" -ForegroundColor Yellow
    exit 1
}

$config = Get-Content $ConfigFile | ConvertFrom-Json
$endpoint = $config.AzureAI.Endpoint
$apiKey = $config.AzureAI.ApiKey

if (-not $endpoint -or -not $apiKey) {
    Write-Host "❌ Missing endpoint or API key in configuration" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Configuration loaded" -ForegroundColor Green
Write-Host "  Endpoint: $endpoint" -ForegroundColor White
Write-Host ""

# Set up headers
$headers = @{
    "api-key" = $apiKey
    "Content-Type" = "application/json"
}

# Function to list recent jobs
function Get-RecentJobs {
    param([int]$Count = 10)
    
    Write-Host "📋 Fetching recent jobs..." -ForegroundColor Yellow
    
    $listUrl = "$($endpoint.TrimEnd('/'))/openai/v1/video/generations/jobs?api-version=preview"
    
    try {
        $response = Invoke-RestMethod -Uri $listUrl -Headers $headers -Method Get
        
        if ($response.data) {
            $jobs = $response.data | Select-Object -First $Count
            
            Write-Host ""
            Write-Host "Recent Jobs ($($jobs.Count)):" -ForegroundColor Cyan
            Write-Host ("=" * 80) -ForegroundColor Cyan
            
            foreach ($job in $jobs) {
                $status = $job.status
                $statusColor = switch ($status) {
                    "succeeded" { "Green" }
                    "failed" { "Red" }
                    "cancelled" { "Yellow" }
                    default { "White" }
                }
                
                Write-Host "Job ID: " -NoNewline -ForegroundColor White
                Write-Host $job.id -ForegroundColor Cyan
                Write-Host "  Status: " -NoNewline -ForegroundColor White
                Write-Host $status -ForegroundColor $statusColor
                
                if ($job.created_at) {
                    $createdDate = [DateTimeOffset]::FromUnixTimeSeconds($job.created_at).LocalDateTime
                    Write-Host "  Created: $($createdDate.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
                }
                
                if ($status -eq "succeeded" -and $job.generations) {
                    Write-Host "  Generations: $($job.generations.Count)" -ForegroundColor Green
                }
                
                Write-Host ""
            }
            
            return $jobs
        } else {
            Write-Host "⚠ No jobs found" -ForegroundColor Yellow
            return @()
        }
    } catch {
        Write-Host "❌ Error fetching jobs: $($_.Exception.Message)" -ForegroundColor Red
        return @()
    }
}

# Function to get job details
function Get-JobDetails {
    param([string]$JobId)
    
    $statusUrl = "$($endpoint.TrimEnd('/'))/openai/v1/video/generations/jobs/$JobId`?api-version=preview"
    
    try {
        $job = Invoke-RestMethod -Uri $statusUrl -Headers $headers -Method Get
        return $job
    } catch {
        Write-Host "❌ Error fetching job $JobId`: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to download video from generation ID
function Download-Video {
    param(
        [string]$GenerationId,
        [string]$OutputPath,
        [string]$JobId
    )
    
    $videoUrl = "$($endpoint.TrimEnd('/'))/openai/v1/video/generations/$GenerationId/content/video?api-version=preview"
    
    Write-Host "  📥 Downloading generation: $GenerationId" -ForegroundColor Cyan
    
    try {
        $response = Invoke-WebRequest -Uri $videoUrl -Headers $headers -Method Get
        
        if ($response.StatusCode -eq 200) {
            [System.IO.File]::WriteAllBytes($OutputPath, $response.Content)
            $fileSizeMB = (Get-Item $OutputPath).Length / 1MB
            Write-Host "  ✓ Downloaded: $OutputPath ($([math]::Round($fileSizeMB, 2)) MB)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  ❌ Download failed: HTTP $($response.StatusCode)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "  ❌ Error downloading: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to download all videos from a job
function Download-JobVideos {
    param(
        [string]$JobId,
        [string]$OutputDir
    )
    
    Write-Host "🎬 Processing job: $JobId" -ForegroundColor Yellow
    
    $job = Get-JobDetails -JobId $JobId
    
    if (-not $job) {
        return
    }
    
    Write-Host "  Status: $($job.status)" -ForegroundColor $(if ($job.status -eq "succeeded") { "Green" } else { "Yellow" })
    
    if ($job.status -ne "succeeded") {
        Write-Host "  ⚠ Job has not succeeded. Status: $($job.status)" -ForegroundColor Yellow
        
        if ($job.status -eq "failed" -and $job.error) {
            Write-Host "  Error: $($job.error.message)" -ForegroundColor Red
        }
        
        return
    }
    
    if (-not $job.generations -or $job.generations.Count -eq 0) {
        Write-Host "  ⚠ No generations found for this job" -ForegroundColor Yellow
        return
    }
    
    Write-Host "  Found $($job.generations.Count) generation(s)" -ForegroundColor Cyan
    
    # Create output directory if it doesn't exist
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    
    $downloadCount = 0
    for ($i = 0; $i -lt $job.generations.Count; $i++) {
        $generation = $job.generations[$i]
        $generationId = $generation.id
        
        # Generate output filename
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $filename = "sora_job_${JobId}_gen_${i}_${timestamp}.mp4"
        $outputPath = Join-Path $OutputDir $filename
        
        if (Download-Video -GenerationId $generationId -OutputPath $outputPath -JobId $JobId) {
            $downloadCount++
        }
    }
    
    Write-Host ""
    Write-Host "✅ Downloaded $downloadCount of $($job.generations.Count) video(s) from job $JobId" -ForegroundColor Green
    Write-Host ""
}

# Main script logic
try {
    if ($ListJobs) {
        # List recent jobs
        $jobs = Get-RecentJobs -Count $LastNJobs
        
        if ($jobs.Count -gt 0) {
            Write-Host "To download videos from a specific job, run:" -ForegroundColor Cyan
            Write-Host "  .\download-sora-videos.ps1 -JobId <job_id>`n" -ForegroundColor White
        }
    }
    elseif ($JobId) {
        # Download videos from specific job
        Download-JobVideos -JobId $JobId -OutputDir $OutputDirectory
    }
    else {
        # Interactive mode - show recent jobs and prompt for selection
        Write-Host "Fetching recent jobs..." -ForegroundColor Yellow
        $jobs = Get-RecentJobs -Count $LastNJobs
        
        if ($jobs.Count -eq 0) {
            Write-Host "No jobs found. Please create a video first." -ForegroundColor Yellow
            exit 0
        }
        
        # Get succeeded jobs only
        $succeededJobs = $jobs | Where-Object { $_.status -eq "succeeded" }
        
        if ($succeededJobs.Count -eq 0) {
            Write-Host "⚠ No succeeded jobs found to download" -ForegroundColor Yellow
            exit 0
        }
        
        Write-Host "Succeeded Jobs Available for Download:" -ForegroundColor Green
        Write-Host ("=" * 80) -ForegroundColor Green
        
        for ($i = 0; $i -lt $succeededJobs.Count; $i++) {
            $job = $succeededJobs[$i]
            Write-Host "[$($i + 1)] " -NoNewline -ForegroundColor Cyan
            Write-Host "Job ID: $($job.id)" -ForegroundColor White
            
            if ($job.created_at) {
                $createdDate = [DateTimeOffset]::FromUnixTimeSeconds($job.created_at).LocalDateTime
                Write-Host "    Created: $($createdDate.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
            }
            
            if ($job.generations) {
                Write-Host "    Generations: $($job.generations.Count)" -ForegroundColor Green
            }
            
            Write-Host ""
        }
        
        Write-Host "Enter job number to download (1-$($succeededJobs.Count)), 'all' for all, or 'q' to quit: " -NoNewline -ForegroundColor Yellow
        $selection = Read-Host
        
        if ($selection -eq 'q') {
            Write-Host "Cancelled." -ForegroundColor Yellow
            exit 0
        }
        elseif ($selection -eq 'all') {
            Write-Host ""
            foreach ($job in $succeededJobs) {
                Download-JobVideos -JobId $job.id -OutputDir $OutputDirectory
            }
        }
        else {
            $index = [int]$selection - 1
            if ($index -ge 0 -and $index -lt $succeededJobs.Count) {
                Write-Host ""
                Download-JobVideos -JobId $succeededJobs[$index].id -OutputDir $OutputDirectory
            } else {
                Write-Host "❌ Invalid selection" -ForegroundColor Red
                exit 1
            }
        }
    }
    
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "Done!" -ForegroundColor Green
    Write-Host "================================" -ForegroundColor Cyan
}
catch {
    Write-Host "❌ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}
