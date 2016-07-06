CREATE DATABASE `TFG` /*!40100 DEFAULT CHARACTER SET latin1 */;
CREATE TABLE `Estacion` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Empresa` varchar(45) NOT NULL,
  `Modelo` varchar(4) NOT NULL,
  `VersionRegistro` int(11) NOT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `ID_UNIQUE` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=latin1;
CREATE TABLE `Evaluacion` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `ID_Estacion` int(11) NOT NULL,
  `Fecha` date NOT NULL,
  `ErrorHardware` tinyint(1) NOT NULL,
  `ErrorRegistro` tinyint(1) NOT NULL,
  `ErrorContadores` tinyint(1) NOT NULL,
  `Solucionado` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`),
  KEY `Estacion_idx` (`ID_Estacion`),
  CONSTRAINT `Evaluacion_Estacion` FOREIGN KEY (`ID_Estacion`) REFERENCES `Estacion` (`ID`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
CREATE TABLE `Hardware` (
  `ID_Estacion` int(11) NOT NULL,
  `Identificador` varchar(45) NOT NULL,
  `Componente` varchar(45) NOT NULL,
  `Sensor` varchar(45) NOT NULL,
  `Minimo` float NOT NULL,
  `Maximo` float NOT NULL,
  `Media` float NOT NULL,
  `Ultimo` float NOT NULL,
  PRIMARY KEY (`ID_Estacion`,`Identificador`),
  CONSTRAINT `Hardware_Estacion` FOREIGN KEY (`ID_Estacion`) REFERENCES `Estacion` (`ID`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
CREATE TABLE `Incidencia` (
  `ID` int(11) NOT NULL,
  `ErrorHardware` varchar(200) DEFAULT NULL,
  `ErrorContadores` varchar(200) DEFAULT NULL,
  `Resuelta` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
CREATE TABLE `Registro` (
  `Modelo` int(11) NOT NULL,
  `Version` varchar(45) NOT NULL,
  `UrlDescarga` varchar(200) NOT NULL,
  `FechaActualizacion` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Modelo`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
CREATE TABLE `Solucion` (
  `ID` int(11) NOT NULL,
  `ID_Incidencia` int(11) NOT NULL,
  `Modelo` varchar(45) NOT NULL,
  `Tipo` varchar(45) NOT NULL,
  `UrlDecarga` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`ID`),
  KEY `Solucion_Incidencia_idx` (`ID_Incidencia`),
  CONSTRAINT `Solucion_Incidencia` FOREIGN KEY (`ID_Incidencia`) REFERENCES `Incidencia` (`ID`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

