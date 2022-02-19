# Install sber-swap pre-requisites for Windows 10 on an Azure VM of type NC (GPU)
# NVIDIA TeslaK80 / Radeon
# https://gist.github.com/thepirat000/ae9c9acf0e9d98b1aa14571cf2197341

Set-ExecutionPolicy Bypass -Scope Process -Force; 

# Install chocolatey
iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'));

refreshenv

# Install GIT
Write-Host "Installing GIT" -foregroundcolor "green";
choco install git -y --no-progress

# Install youtube-dl
Write-Host "Installing youtube-dl" -foregroundcolor "green";
choco install youtube-dl -y --no-progress

# Install yt-dlp
Write-Host "Installing yt-dlp" -foregroundcolor "green";
choco install yt-dlp -y --no-progress

# Install ffmpeg
Write-Host "Installing ffmpeg" -foregroundcolor "green";
choco install ffmpeg -y --no-progress

# Install wget
Write-Host "Installing wget" -foregroundcolor "green";
remove-item alias:wget
choco install wget -y --no-progress

# Install CUDA drivers
# NOTE: Make sure to uninstall any other CUDA version than the required on requirements.txt
Write-Host "Installing CUDA drivers (this can take some time)" -foregroundcolor "green";
choco install cuda --version=10.1 -y --no-progress

refreshenv

# Install newer NVidia Tesla driver (this step requires user interaction)
$file = './42.50-tesla-desktop-win10-64bit-international.exe';
Start-BitsTransfer -Source http://us.download.nvidia.com/tesla/442.50/442.50-tesla-desktop-win10-64bit-international.exe -Destination $file
& $file -y --no-progress

pause

# Configure GPU
& "C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi" -fdm 0

# Install Conda
Write-Host "Installing miniconda3 (this can take some time)" -foregroundcolor "green";
choco install miniconda3 -y --no-progress

# Create conda environment
& 'C:\tools\miniconda3\shell\condabin\conda-hook.ps1'; 
conda activate 'C:\tools\miniconda3';
conda update -n base -c defaults conda -y
conda create -n sber python=3.7 conda -y
conda activate sber

# Clone and prepare project
mkdir C:\GIT
cd C:\GIT

git clone https://github.com/sberbank-ai/sber-swap.git
cd sber-swap
git submodule init
git submodule update

# Install sber-swap
pip install pip --upgrade

# Workaround to fix requirements.txt (remove requests lib version)
$file = 'requirements.txt'
$regex = 'requests==.*'
(Get-Content $file) -replace $regex, 'requests' | Set-Content $file

# Install dependencies
pip install -r requirements.txt

# Download models
ren download_models.sh download_models.cmd
& ./download_models.cmd

# Test
python inference.py --source_paths examples/images/elon_musk.jpg --target_faces_paths examples/images/tgt1.png --target_video examples/videos/dirtydancing.mp4

# Deactivate conda
conda deactivate 
conda deactivate 

Write-Host "Done..." -foregroundcolor "green";